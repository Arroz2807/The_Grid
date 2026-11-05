
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// Administra la comunicación serial con Arduino.
///
/// Es el único componente responsable de abrir, leer y cerrar
/// el puerto COM. Recibe comandos L, R, U y D y los deja
/// disponibles para los sistemas de entrada de Unity.
///
/// La lectura se realiza en un hilo secundario para evitar
/// bloquear el hilo principal del juego.
/// </summary>
public class ArduinoSerialManager : MonoBehaviour
{
    public static ArduinoSerialManager Instance { get; private set; }

    [Header("Configuración serial")]
    [Tooltip("Puerto COM al que está conectado el Arduino. Ejemplo: COM3.")]
    [SerializeField] private string portName = "COM3";

    [Tooltip("Debe coincidir con Serial.begin() del programa Arduino.")]
    [SerializeField] private int baudRate = 9600;

    [Tooltip("Tiempo máximo de espera de una lectura serial, en milisegundos.")]
    [SerializeField] private int readTimeout = 100;

    [Header("Diagnóstico")]
    [SerializeField] private bool showConnectionLogs = true;

    // Objeto encargado de comunicarse con el puerto serial.
    private SerialPort serialPort;

    // Hilo que lee los mensajes sin bloquear el juego.
    private Thread readThread;

    private volatile bool isRunning;

    // Cola compartida entre el hilo serial y el hilo principal de Unity.
    private readonly Queue<char> receivedCommands = new Queue<char>();
    private readonly object queueLock = new object();

    // Los errores se guardan en el hilo serial y se informan desde Update.
    private string pendingError;
    private readonly object errorLock = new object();

    private const int MaxQueuedCommands = 16;

    private void Awake()
    {
        // Garantiza que exista una sola instancia del administrador.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Permite conservar la conexión al cambiar de escena.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        OpenConnection();
    }

    /// <summary>
    /// Abre el puerto COM e inicia el hilo de lectura.
    /// </summary>
    private void OpenConnection()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = readTimeout;
            serialPort.NewLine = "\n";

            serialPort.Open();

            // Descarta datos antiguos que pudieran haber quedado en el puerto.
            serialPort.DiscardInBuffer();

            isRunning = true;

            readThread = new Thread(ReadSerialData);
            readThread.IsBackground = true;
            readThread.Start();

            if (showConnectionLogs)
            {
                Debug.Log(
                    $"[ArduinoSerialManager] Conectado a {portName} " +
                    $"a {baudRate} baudios."
                );
            }
        }
        catch (Exception exception)
        {
            isRunning = false;

            Debug.LogError(
                $"[ArduinoSerialManager] No se pudo abrir {portName}. " +
                $"Verificá el puerto COM, que Arduino esté conectado y " +
                $"que ningún otro programa esté usando el puerto. " +
                $"Detalle: {exception.Message}"
            );

            CloseConnection();
        }
    }

    /// <summary>
    /// Lee las líneas que envía Arduino.
    ///
    /// Este método se ejecuta en un hilo secundario.
    /// No se debe utilizar aquí la API de Unity, como Debug.Log
    /// o funciones de objetos de la escena.
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

                if (command != 'L' &&
                    command != 'R' &&
                    command != 'U' &&
                    command != 'D')
                {
                    continue;
                }

                lock (queueLock)
                {
                    // Evita acumular una cantidad ilimitada de comandos.
                    // Si Unity no los consumió a tiempo, descartamos el más viejo.
                    while (receivedCommands.Count >= MaxQueuedCommands)
                    {
                        receivedCommands.Dequeue();
                    }

                    receivedCommands.Enqueue(command);
                }
            }
            catch (TimeoutException)
            {
                // Es normal: permite comprobar periódicamente si debemos detenernos.
            }
            catch (InvalidOperationException)
            {
                // El puerto se cerró mientras se estaba leyendo.
                break;
            }
            catch (IOException exception)
            {
                StoreError($"Error de lectura serial: {exception.Message}");
                break;
            }
            catch (Exception exception)
            {
                if (isRunning)
                {
                    StoreError($"Error serial: {exception.Message}");
                }

                break;
            }
        }
    }

    /// <summary>
    /// Devuelve el comando más reciente disponible.
    ///
    /// Si llegaron varios comandos antes de que Unity los procesara,
    /// consume todos y devuelve el último para evitar giros atrasados.
    /// </summary>
    public bool TryGetLatestCommand(out char command)
    {
        command = '\0';
        bool hasCommand = false;

        lock (queueLock)
        {
            while (receivedCommands.Count > 0)
            {
                command = receivedCommands.Dequeue();
                hasCommand = true;
            }
        }

        return hasCommand;
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
            Debug.LogError($"[ArduinoSerialManager] {errorToReport}");
        }
    }

    /// <summary>
    /// Detiene el hilo y libera el puerto serial.
    /// </summary>
    private void CloseConnection()
    {
        isRunning = false;

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
                    $"[ArduinoSerialManager] Error al cerrar el puerto: " +
                    exception.Message
                );
            }
        }

        // La lectura tiene un timeout, por lo que el hilo debería terminar
        // sin permanecer bloqueado indefinidamente.
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
        // Una instancia duplicada no debe cerrar la conexión de la original.
        if (Instance != this)
            return;

        CloseConnection();
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (Instance == this)
        {
            CloseConnection();
        }
    }
}
