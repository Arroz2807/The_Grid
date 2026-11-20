
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// Administra la comunicación serial con Arduino.
///
/// Mantiene cuatro datos independientes:
/// - Dirección actual del joystick.
/// - Cola de cambios de dirección para el movimiento del ciclo.
/// - Confirmaciones pendientes del pulsador.
/// - Cola tipada de actividad para el Attract Mode.
///
/// La lectura serial se realiza en un hilo secundario.
///
/// También administra la reconexión automática cuando Arduino
/// no está disponible o se interrumpe la comunicación.
/// </summary>
public class ArduinoSerialManager : MonoBehaviour
{
    public static ArduinoSerialManager Instance { get; private set; }

    /// <summary>Estado legible para diagnóstico (y para un futuro cartel en pantalla).</summary>
    public static string ConnectionStatus { get; private set; } = "Sin conectar";

    /// <summary>True cuando el puerto serial está abierto (no garantiza que haya joystick).</summary>
    public static bool IsSerialPortOpen { get; private set; }

    [Header("Configuración serial")]
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int readTimeout = 100;

    [Header("Reconexión automática")]
    [Tooltip("Segundos entre intentos de reconexión.")]
    [SerializeField] private float reconnectInterval = 2.5f;

    [Header("Autodetección de puerto")]
    [Tooltip("Si el puerto configurado falla, probar todos los puertos seriales disponibles.")]
    [SerializeField] private bool autoDetectPort = true;

    [Tooltip("Segundos sin recibir ningún comando válido antes de probar otro puerto.")]
    [SerializeField] private float silentPortTimeout = 10f;

    [Header("Diagnóstico")]
    [SerializeField] private bool showConnectionLogs = true;

    [Tooltip("Muestra un cartel ARDUINO: CONECTADO / NO CONECTADO (desactivado por defecto).")]
    [SerializeField] private bool showConnectionStatusOnScreen = false;

    private SerialPort serialPort;
    private Thread readThread;

    private volatile bool isRunning;
    private volatile bool connectionLost;

    // El puerto configurado (COM3) siempre es el primero de la lista.
    private readonly List<string> candidatePorts = new List<string>();
    private int candidateIndex = -1;
    private int attemptsSinceRefresh;
    private string connectedPort;

    // Se pone en true apenas llega un comando válido: desde ahí el puerto
    // queda fijado por el resto de la sesión.
    private volatile bool portConfirmed;
    private float connectedAtTime;

    private float nextReconnectTime;
    private bool hasReportedConnectionFailure;

    // Cola de direcciones para el movimiento del ciclo.
    private readonly Queue<char> receivedDirections = new Queue<char>();
    private readonly object directionLock = new object();

    // Estado físico actual del joystick.
    private char heldDirection = 'N';

    // Confirmaciones pendientes del pulsador.
    private int pendingConfirmations = 0;

    // Cola independiente de actividad para el Attract Mode.
    // L/R/U/D = dirección del joystick.
    // C = pulsador.
    // N = joystick volvió a neutral.
    private readonly Queue<char> pendingActivity = new Queue<char>();
    private readonly object activityLock = new object();

    // Sincroniza los datos compartidos entre hilos.
    private readonly object inputLock = new object();

    private string pendingError;
    private readonly object errorLock = new object();

    private const int MaxQueuedDirections = 16;
    private const int MaxQueuedActivity = 32;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Armamos la lista antes del primer intento. Así COM3 se prueba
        // primero, exactamente igual que antes.
        RefreshCandidatePorts();

        // Primer intento de conexión.
        OpenConnection();
    }

    /// <summary>
    /// Reconstruye la lista de puertos: primero el configurado en Inspector
    /// (COM3) y luego todos los que reporte el sistema. Se vuelve a llamar
    /// cuando se agotó la lista, para detectar Arduino enchufado en caliente.
    /// </summary>
    private void RefreshCandidatePorts()
    {
        candidatePorts.Clear();

        if (!string.IsNullOrEmpty(portName))
            candidatePorts.Add(portName);

        if (autoDetectPort)
        {
            string[] systemPorts;

            try
            {
                systemPorts = SerialPort.GetPortNames();
            }
            catch
            {
                systemPorts = new string[0];
            }

            if (systemPorts != null)
            {
                foreach (string port in systemPorts)
                {
                    if (string.IsNullOrEmpty(port))
                        continue;

                    if (!candidatePorts.Contains(port))
                        candidatePorts.Add(port);
                }
            }
        }

        candidateIndex = -1;
        attemptsSinceRefresh = 0;
    }

    private static bool IsValidCommand(char command)
    {
        return command == 'L' || command == 'R' ||
               command == 'U' || command == 'D' ||
               command == 'N' || command == 'C';
    }

    /// <summary>
    /// Intenta abrir el puerto COM e iniciar el hilo de lectura.
    /// Si falla, programa un nuevo intento con el siguiente candidato.
    /// </summary>
    private void OpenConnection()
    {
        // Evitar abrir una segunda conexión si ya existe una activa.
        if (isRunning)
            return;

        // Después de recorrer todos los candidatos, volvemos a consultar el
        // sistema por si enchufaron el Arduino mientras tanto.
        if (candidatePorts.Count == 0 || attemptsSinceRefresh >= candidatePorts.Count)
            RefreshCandidatePorts();

        if (candidatePorts.Count == 0)
        {
            ConnectionStatus = "Sin puertos seriales disponibles";
            nextReconnectTime = Time.time + Mathf.Max(0.5f, reconnectInterval);
            return;
        }

        attemptsSinceRefresh++;
        candidateIndex = (candidateIndex + 1) % candidatePorts.Count;
        string selectedPort = candidatePorts[candidateIndex];

        try
        {
            serialPort = new SerialPort(selectedPort, baudRate);
            serialPort.ReadTimeout = readTimeout;
            serialPort.NewLine = "\n";

            serialPort.Open();
            serialPort.DiscardInBuffer();

            ResetInputState();

            connectedPort = selectedPort;
            portConfirmed = false;
            connectedAtTime = Time.time;
            connectionLost = false;
            isRunning = true;
            IsSerialPortOpen = true;
            ConnectionStatus = "Puerto " + selectedPort + " abierto; esperando joystick";

            readThread = new Thread(ReadSerialData);
            readThread.IsBackground = true;
            readThread.Start();

            hasReportedConnectionFailure = false;
            nextReconnectTime = 0f;

            if (showConnectionLogs)
            {
                Debug.Log(
                    $"[ArduinoSerialManager] Conectado a {selectedPort} " +
                    $"a {baudRate} baudios."
                );
            }
        }
        catch (Exception exception)
        {
            isRunning = false;
            IsSerialPortOpen = false;

            CloseConnection();

            nextReconnectTime = Time.time + Mathf.Max(0.5f, reconnectInterval);
            ConnectionStatus = "Sin Arduino (probando puertos)";

            // Evitar llenar la consola con el mismo error cada pocos segundos.
            if (showConnectionLogs && !hasReportedConnectionFailure)
            {
                Debug.LogWarning(
                    $"[ArduinoSerialManager] No se pudo conectar a {selectedPort}. " +
                    "Se probarán los demás puertos automáticamente. " +
                    $"Detalle: {exception.Message}"
                );

                hasReportedConnectionFailure = true;
            }
        }
    }

    /// <summary>
    /// Lee los mensajes enviados por Arduino.
    /// Se ejecuta en un hilo secundario.
    /// </summary>
    private void ReadSerialData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string message = serialPort.ReadLine();

                if (string.IsNullOrWhiteSpace(message))
                    continue;

                char command = char.ToUpperInvariant(message.Trim()[0]);

                // Primera señal válida: confirmamos que este puerto es el del
                // Arduino y dejamos de rotar candidatos.
                if (!portConfirmed && IsValidCommand(command))
                {
                    portConfirmed = true;
                    ConnectionStatus = "Arduino conectado en " + connectedPort;

                    if (showConnectionLogs)
                    {
                        Debug.Log(
                            "[ArduinoSerialManager] Joystick confirmado en " +
                            connectedPort + "."
                        );
                    }
                }

                switch (command)
                {
                    case 'L':
                    case 'R':
                    case 'U':
                    case 'D':
                        RegisterDirection(command);
                        break;

                    case 'N':
                        RegisterNeutral();
                        break;

                    case 'C':
                        RegisterConfirmation();
                        break;
                }
            }
            catch (TimeoutException)
            {
                // Es normal: permite comprobar periódicamente si debemos detenernos.
            }
            catch (InvalidOperationException)
            {
                if (isRunning)
                    RegisterConnectionLoss("El puerto serial dejó de estar disponible.");

                break;
            }
            catch (IOException exception)
            {
                RegisterConnectionLoss(
                    $"Se interrumpió la comunicación serial: {exception.Message}"
                );

                break;
            }
            catch (Exception exception)
            {
                if (isRunning)
                {
                    RegisterConnectionLoss(
                        $"Error inesperado de comunicación: {exception.Message}"
                    );
                }

                break;
            }
        }
    }

    /// <summary>
    /// Informa al hilo principal que la comunicación se perdió.
    /// No cierra el puerto desde el hilo secundario.
    /// </summary>
    private void RegisterConnectionLoss(string message)
    {
        if (!isRunning)
            return;

        StoreError(message);

        connectionLost = true;
        isRunning = false;
    }

    /// <summary>
    /// Registra una nueva dirección del joystick.
    /// </summary>
    private void RegisterDirection(char direction)
    {
        lock (inputLock)
        {
            heldDirection = direction;
        }

        lock (directionLock)
        {
            while (receivedDirections.Count >= MaxQueuedDirections)
            {
                receivedDirections.Dequeue();
            }

            receivedDirections.Enqueue(direction);
        }

        RegisterActivity(direction);
    }

    /// <summary>
    /// Registra que el joystick volvió al centro.
    /// No modifica las colas de movimiento.
    /// </summary>
    private void RegisterNeutral()
    {
        lock (inputLock)
        {
            heldDirection = 'N';
        }

        RegisterActivity('N');
    }

    /// <summary>
    /// Registra una confirmación del pulsador.
    /// </summary>
    private void RegisterConfirmation()
    {
        lock (inputLock)
        {
            pendingConfirmations++;
        }

        RegisterActivity('C');
    }

    /// <summary>
    /// Registra un evento tipado para el Attract Mode.
    /// Se ejecuta desde el hilo serial.
    /// </summary>
    private void RegisterActivity(char activity)
    {
        lock (activityLock)
        {
            while (pendingActivity.Count >= MaxQueuedActivity)
            {
                pendingActivity.Dequeue();
            }

            pendingActivity.Enqueue(activity);
        }
    }

    /// <summary>
    /// Consume un evento de actividad del Arduino.
    /// L/R/U/D = joystick, C = pulsador, N = neutral.
    /// </summary>
    public bool TryConsumeActivity(out char activity)
    {
        lock (activityLock)
        {
            if (pendingActivity.Count <= 0)
            {
                activity = '\0';
                return false;
            }

            activity = pendingActivity.Dequeue();
            return true;
        }
    }

    /// <summary>
    /// Descarta todas las actividades pendientes.
    /// No modifica heldDirection ni las otras colas.
    /// </summary>
    public void ClearPendingActivity()
    {
        lock (activityLock)
        {
            pendingActivity.Clear();
        }
    }

    /// <summary>
    /// Devuelve la dirección que el joystick mantiene actualmente.
    /// N significa que el joystick está neutral.
    /// </summary>
    public char GetHeldDirection()
    {
        lock (inputLock)
        {
            return heldDirection;
        }
    }

    /// <summary>
    /// Entrega una confirmación pendiente.
    /// Cada pulsación se entrega una sola vez.
    /// </summary>
    public bool TryGetConfirmCommand()
    {
        lock (inputLock)
        {
            if (pendingConfirmations <= 0)
                return false;

            pendingConfirmations--;
            return true;
        }
    }

    /// <summary>
    /// Consume los cambios de dirección pendientes y devuelve
    /// solamente el más reciente.
    /// </summary>
    public bool TryGetLatestCommand(out char command)
    {
        command = '\0';
        bool hasCommand = false;

        lock (directionLock)
        {
            while (receivedDirections.Count > 0)
            {
                command = receivedDirections.Dequeue();
                hasCommand = true;
            }
        }

        return hasCommand;
    }

    /// <summary>
    /// Restablece las entradas pendientes.
    /// Se utiliza al conectar o perder la comunicación.
    /// </summary>
    private void ResetInputState()
    {
        lock (inputLock)
        {
            heldDirection = 'N';
            pendingConfirmations = 0;
        }

        lock (directionLock)
        {
            receivedDirections.Clear();
        }

        ClearPendingActivity();
    }

    private void StoreError(string message)
    {
        lock (errorLock)
        {
            pendingError = message;
        }
    }

    private void Update()
    {
        // 1. Informar errores detectados por el hilo serial.
        string errorToReport = null;

        lock (errorLock)
        {
            if (!string.IsNullOrEmpty(pendingError))
            {
                errorToReport = pendingError;
                pendingError = null;
            }
        }

        if (errorToReport != null)
        {
            Debug.LogWarning(
                $"[ArduinoSerialManager] {errorToReport}"
            );
        }

        // 2. Procesar una desconexión detectada.
        if (connectionLost)
        {
            connectionLost = false;

            CloseConnection();
            ResetInputState();

            nextReconnectTime = Time.time + Mathf.Max(0.5f, reconnectInterval);

            if (showConnectionLogs)
            {
                Debug.LogWarning(
                    "[ArduinoSerialManager] Conexión perdida. " +
                    $"Se intentará reconectar en {reconnectInterval:F1} segundos."
                );
            }
        }

        // 3. Si no hay conexión, intentar recuperarla periódicamente.
        if (!isRunning && Time.time >= nextReconnectTime)
        {
            OpenConnection();
        }

        // 4. Un puerto puede abrir y NO ser el Arduino (otro dispositivo
        //    serial). Si nunca llegó un comando válido, probamos el siguiente.
        //    En cuanto llega uno, portConfirmed queda en true y se termina.
        if (isRunning && autoDetectPort && candidatePorts.Count > 1 &&
            !portConfirmed && Time.time - connectedAtTime >= silentPortTimeout)
        {
            string silentPort = connectedPort;

            ConnectionStatus = "Sin joystick en " + silentPort + "; probando otro puerto";

            CloseConnection();
            ResetInputState();

            // El siguiente OpenConnection elige el siguiente candidato.
            nextReconnectTime = Time.time;
        }
    }

    /// <summary>
    /// Cartel opcional de diagnóstico. Desactivado por defecto para no
    /// modificar la presentación del juego.
    /// </summary>
    private void OnGUI()
    {
        if (!showConnectionStatusOnScreen)
            return;

        const int width = 360;
        const int height = 26;

        string text = portConfirmed
            ? "ARDUINO: CONECTADO"
            : "ARDUINO: NO CONECTADO (teclado activo)";

        GUI.Label(
            new Rect(10, Screen.height - height - 10, width, height),
            text
        );
    }

    /// <summary>
    /// Detiene el hilo y libera el puerto serial.
    /// Se ejecuta desde el hilo principal de Unity.
    /// </summary>
    private void CloseConnection()
    {
        isRunning = false;
        IsSerialPortOpen = false;

        if (serialPort != null)
        {
            try
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[ArduinoSerialManager] Error al cerrar el puerto: " +
                    exception.Message
                );
            }
        }

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(250);
        }

        if (serialPort != null)
        {
            serialPort.Dispose();
            serialPort = null;
        }

        readThread = null;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        CloseConnection();
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (Instance == this)
            CloseConnection();
    }
}
