using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Valmon.Gameplay;

namespace Valmon.Network
{
    public enum NetworkRole
    {
        Offline,
        Host,   // Usually Tamer initially
        Client  // Usually Warden initially
    }

    [Serializable]
    public class NetworkPacket
    {
        public string type; // "POS", "CORRUPT", "TRAP", "CATCH", "ROLESWAP", "SCORE"
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public int targetIndex;
        public int scoreTamer;
        public int scoreWarden;
        public float timer;
    }

    public class ValmonNetworkManager : MonoBehaviour
    {
        public static ValmonNetworkManager Instance { get; private set; }

        [Header("Network Configuration")]
        public NetworkRole currentRole = NetworkRole.Offline;
        public int port = 7777;
        public string connectAddress = "127.0.0.1";
        public bool isConnected = false;

        // TCP Sockets
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private Thread listenThread;
        private Thread receiveThread;
        private bool isRunning = false;

        // Sync intervals
        private float lastPosSyncTime = 0f;
        private const float SyncRate = 0.05f; // 20 times per second

        // Thread-safe message queue
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> messageQueue = 
            new System.Collections.Concurrent.ConcurrentQueue<string>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Update()
        {
            // Process incoming network messages on main thread
            while (messageQueue.TryDequeue(out string json))
            {
                ProcessPacket(json);
            }

            // Sync Tamer position if Host
            if (isConnected && currentRole == NetworkRole.Host && Time.time - lastPosSyncTime >= SyncRate)
            {
                lastPosSyncTime = Time.time;
                SendTamerPosition();
            }
        }

        public void StartHost()
        {
            Disconnect();
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                isRunning = true;
                currentRole = NetworkRole.Host;

                listenThread = new Thread(ListenForClients);
                listenThread.IsBackground = true;
                listenThread.Start();

                Debug.Log($"<color=#55ff99><b>[Network]</b> Host server started on port {port}. Waiting for Warden client to connect...</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Failed to start host: {e.Message}");
            }
        }

        private void ListenForClients()
        {
            try
            {
                tcpClient = tcpListener.AcceptTcpClient();
                networkStream = tcpClient.GetStream();
                isConnected = true;
                Debug.Log("<color=#55ff99><b>[Network]</b> Client connected! Match synchronized.</color>");

                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception e)
            {
                if (isRunning) Debug.LogWarning($"[Network] Listener stopped: {e.Message}");
            }
        }

        public void ConnectToHost(string ip)
        {
            Disconnect();
            try
            {
                connectAddress = ip;
                tcpClient = new TcpClient();
                tcpClient.Connect(connectAddress, port);
                networkStream = tcpClient.GetStream();
                isConnected = true;
                isRunning = true;
                currentRole = NetworkRole.Client;

                // Client starts as Warden
                GameManager.Instance?.ToggleRole();

                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                Debug.Log($"<color=#55ff99><b>[Network]</b> Successfully connected to host at {connectAddress}:{port}!</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Failed to connect to host: {e.Message}");
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            while (isRunning && networkStream != null && tcpClient != null && tcpClient.Connected)
            {
                try
                {
                    int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Disconnected

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(chunk);

                    string data = sb.ToString();
                    int newlineIndex;
                    while ((newlineIndex = data.IndexOf('\n')) != -1)
                    {
                        string line = data.Substring(0, newlineIndex).Trim();
                        data = data.Substring(newlineIndex + 1);
                        if (!string.IsNullOrEmpty(line))
                        {
                            messageQueue.Enqueue(line);
                        }
                    }
                    sb.Clear();
                    sb.Append(data);
                }
                catch
                {
                    break;
                }
            }

            isConnected = false;
        }

        public void SendPacket(NetworkPacket packet)
        {
            if (!isConnected || networkStream == null) return;

            try
            {
                string json = JsonUtility.ToJson(packet) + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                networkStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Network] Send error: {e.Message}");
            }
        }

        private void SendTamerPosition()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            NetworkPacket packet = new NetworkPacket
            {
                type = "POS",
                posX = player.transform.position.x,
                posY = player.transform.position.y,
                posZ = player.transform.position.z,
                rotY = player.transform.eulerAngles.y
            };
            SendPacket(packet);
        }

        private void ProcessPacket(string json)
        {
            try
            {
                NetworkPacket packet = JsonUtility.FromJson<NetworkPacket>(json);
                if (packet == null) return;

                switch (packet.type)
                {
                    case "POS":
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null && currentRole == NetworkRole.Client)
                        {
                            player.transform.position = Vector3.Lerp(player.transform.position, new Vector3(packet.posX, packet.posY, packet.posZ), 0.5f);
                            player.transform.rotation = Quaternion.Euler(0, packet.rotY, 0);
                        }
                        break;

                    case "CORRUPT":
                        CreatureController[] creatures = FindObjectsByType<CreatureController>(FindObjectsSortMode.None);
                        if (packet.targetIndex >= 0 && packet.targetIndex < creatures.Length)
                        {
                            creatures[packet.targetIndex].SetCorrupted(true);
                        }
                        break;

                    case "CATCH":
                        GameManager.Instance?.AddScore(isTamer: true, points: 150);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Network] Packet parse error: {e.Message}");
            }
        }

        public void Disconnect()
        {
            isRunning = false;
            isConnected = false;

            try { networkStream?.Close(); } catch { }
            try { tcpClient?.Close(); } catch { }
            try { tcpListener?.Stop(); } catch { }

            currentRole = NetworkRole.Offline;
        }

        void OnDestroy()
        {
            Disconnect();
        }

        void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
