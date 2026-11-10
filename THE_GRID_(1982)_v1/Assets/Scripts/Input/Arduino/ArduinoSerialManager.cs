
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
/// el puerto COM.
///
/// Mantiene tres datos independientes:
/// - Dirección actual del joystick.
/// - Cola de cambios de dirección para el movimiento del ciclo.
/// - Confirmaciones pendientes del pulsador.
///
/// La lectura serial se realiza en un hilo secundario.
/// </summary>
public class ArduinoSerialManager : MonoBehaviour
{
    public static ArduinoSerialManager Instance { get; private set; }

    [Header("Configuración serial")]
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int readTimeout = 100;

    [Header("Diagnóstico")]
    [SerializeField] private bool showConnectionLogs = true;

    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool isRunning;

    // Cola de direcciones para el movimiento del ciclo.
    private readonly Queue<char> receivedDirections = new Queue<char>();
    private readonly object directionLock = new object();

    // Estado actual del joystick para la navegación de menús.
    private char heldDirection = 'N';

    // Cantidad de confirmaciones pendientes.
    private int pendingConfirmations = 0;

    // Sincroniza los datos compartidos entre hilos.
    private readonly object inputLock = new object();

    private string pendingError;
    private readonly object errorLock = new object();

    private const int MaxQueuedDirections = 16;

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
            serialPort.DiscardInBuffer();

            lock (inputLock)
            {
                heldDirection = 'N';
                pendingConfirmations = 0;
            }

            lock (directionLock)
            {
                receivedDirections.Clear();
            }

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
                    StoreError($"Error serial: {exception.Message}");

                break;
            }
        }
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
    }

    /// <summary>
    /// Registra que el joystick volvió al centro.
    /// </summary>
    private void RegisterNeutral()
    {
        lock (inputLock)
        {
            heldDirection = 'N';
        }
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
    }

    /// <summary>
    /// Devuelve la dirección que el joystick mantiene actualmente.
    ///
    /// No consume el estado: se puede consultar en cada frame.
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
    ///
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
    ///
    /// Se utiliza para el movimiento del ciclo durante la partida.
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
