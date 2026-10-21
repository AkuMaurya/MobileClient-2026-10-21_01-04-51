using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TCPCarClient : MonoBehaviour
{
    [Header("Server Connection Settings")]
    public string serverIP = "10.105.208.233";
    public int port = 5000;
    public InputField ipInputField;
    [Header("Car Controls")]
    [Range(-1f, 1f)] public float steer;
    [Range(0f, 1f)] public float throttle;
    [Range(0f, 1f)] public float brake;

    public int gear = 1;
    public int focus;

    // Controls whether the networking thread should continue running.
    private volatile bool running;

    // Performs network operations separately from Unity's main thread.
    private Thread networkThread;
    private TcpClient client;

    // Protects the latest message when the main thread and network thread access it.
    private readonly object sendLock = new object();
    private string latestMessage;

    // True when the TCP connection has been established.
    private volatile bool connected;
    bool startClient = false; 
    void Start()
    {
        focus = -1;
    }
    public void MakeConnection()
    {
        startClient = true;
        serverIP = ipInputField.text;
        Debug.Log("Network [CLIENT] Starting TCP client...");
        running = true; 
        // Start the networking thread.
        networkThread = new Thread(NetworkLoop);
        networkThread.IsBackground = true;
        networkThread.Start(); 
        Debug.Log("Network [CLIENT] Network thread started.");
    }

    void Update()
    {
        if (!startClient)
            return;
        // Collect the latest car-control values every Unity frame.
        NetworkMessage msg = new NetworkMessage
        {
            steer = steer,
            throttle = throttle,
            brake = brake,
            gear = gear,
            focus = focus
        };

        // Convert the controls into JSON and add a newline.
        // The newline is important because the server uses ReadLine().
        string json = JsonUtility.ToJson(msg) + "\n";

        lock (sendLock)
        {
            latestMessage = json;
        }
    }
    
    void NetworkLoop()
    {
        while (running)
        {
            try
            {
                Debug.Log(
                    $"Network [CLIENT] Attempting connection to {serverIP}:{port}..."
                );

                // Create a TCP client and disable Nagle's algorithm.
                client = new TcpClient();
                client.NoDelay = true;

                // Attempt to connect to the server.
                // This throws an exception if the connection fails.
                client.Connect(serverIP, port);

                // Reaching this line means the TCP connection succeeded.
                Debug.Log("Network [CLIENT] TCP CONNECTION SUCCESSFUL!");

                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer =
                       new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.AutoFlush = true;

                    connected = true; 
                    Debug.Log("Network [CLIENT] Ready to send car-control data.");

                    float lastStatusLog = Time.realtimeSinceStartup;

                    while (running && client.Connected)
                    {
                        string message;

                        // Safely read the most recently prepared message.
                        lock (sendLock)
                        {
                            message = latestMessage;
                        }

                        // Send the latest controls to the server.
                        if (!string.IsNullOrEmpty(message))
                        {
                            writer.Write(message);
                        }

                        // Log periodically instead of flooding the Console.
                        if (Time.realtimeSinceStartup - lastStatusLog >= 2f)
                        {
                            Debug.Log(
                                "Network [CLIENT] Connected; sending car-control data."
                            );

                            lastStatusLog = Time.realtimeSinceStartup;
                        }

                        // Send updates approximately every 50 milliseconds.
                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception e)
            {
                // This message explains why a connection or send operation failed.
                Debug.LogError(
                    $"Network [CLIENT] CONNECTION/SEND ERROR: {e.GetType().Name}: {e.Message}"
                );
            }
            finally
            {
                // Mark the client as disconnected regardless of the outcome.
                connected = false;

                try
                {
                    client?.Close();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Network [CLIENT] Error closing socket: " + e.Message);
                }

                client = null;

                Debug.Log("Network [CLIENT] Disconnected from server.");
            }

            // Retry after one second if the application is still running.
            if (running)
            {
                Debug.Log("Network [CLIENT] Retrying connection in 1 second...");
                Thread.Sleep(1000);
            }
        }

        Debug.Log("Network [CLIENT] Network thread stopped.");
    } 

    // Optional: call this from the UI to check connection status.
    public bool IsConnected()
    {
        return connected;
    }

    public void SetSteeringLeft()
    {
        steer = -1f;
        Debug.Log("Network [CLIENT] Steering = LEFT (-1)");
    }

    public void SetSteeringRight()
    {
        steer = 1f;
        Debug.Log("Network [CLIENT] Steering = RIGHT (+1)");
    }

    public void SetSteeringNeutral()
    {
        steer = 0f;
        Debug.Log("Network [CLIENT] Steering = NEUTRAL (0)");
    }

    // Call these from Button Pointer Down / Pointer Up events.
    public void ThrottleDown()
    {
        throttle = 1f;
        Debug.Log("Network [CLIENT] Throttle pressed.");
    }

    public void ThrottleUp()
    {
        throttle = 0f;
        Debug.Log("Network [CLIENT] Throttle released.");
    }

    public void BrakeDown()
    {
        brake = 1f;
        Debug.Log("Network [CLIENT] Brake pressed.");
    }

    public void BrakeUp()
    {
        brake = 0f;
        Debug.Log("Network [CLIENT] Brake released.");
    }

    public void SelectDrive(Slider slider)
    {
        gear = (int)slider.value;
        Debug.Log("Network [CLIENT] Gear = DRIVE");
    }

    public void SelectNeutral()
    {
        gear = 0;
        Debug.Log("Network [CLIENT] Gear = NEUTRAL");
    }

    public void SelectReverse()
    {
        gear = -1;
        Debug.Log("Network [CLIENT] Gear = REVERSE");
    }

    public void SelectFocus(int index)
    {
        focus++;
        focus = focus % 5; // Cycle through 0, 1, 2
        Debug.Log("Network [CLIENT] Focus = " + focus);
    }

    void OnDestroy()
    {
        Debug.Log("Network [CLIENT] Shutting down TCP client...");

        running = false;
        connected = false;

        // Closing the socket helps unblock a pending network operation.
        try
        {
            client?.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Network [CLIENT] Shutdown warning: " + e.Message);
        }

        // Wait briefly for the networking thread to finish.
        if (networkThread != null && networkThread.IsAlive)
        {
            networkThread.Join(500);
        }

        Debug.Log("Network [CLIENT] TCP client shutdown complete.");
    }
}