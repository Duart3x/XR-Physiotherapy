using K4AdotNet.BodyTracking;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using UnityEngine;

namespace K4AdotNet.Samples.Unity
{

    public class SkeletonProvider : MonoBehaviour, IInitializable
    {
        // ----------------------------------------------
        // CHANGES REGARDING ORIGINAL SKELETON PROVIDER | 
        // ---------------------------------------------------------------------------------------
        // REMOVED: private Tracker _tracker;                                                    |
        // REPLACED: All the SDK initialization and tracker setup with socket connection         |
        // CURRENT STATE: Now all data is received via TCP socket from an external Kinect client |
        // ---------------------------------------------------------------------------------------
        
        private Socket serverSocket;
        private Socket clientSocket;
        private const int BUFFER_SIZE = 4096;
        private const string HOST = "10.182.54.240";
        private const int PORT = 8888;
        private string buffer = "";
        private int frameCount = 0;
        private byte[] receiveBuffer = new byte[BUFFER_SIZE];


        public bool IsInitializationComplete { get; private set; }
        public bool IsAvailable { get; private set; }
        public event EventHandler<SkeletonEventArgs> SkeletonUpdated;


        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.1f);

            IsInitializationComplete = false;
            IsAvailable = false;

            // Create a TCP/IP socket
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Allow reuse of address
            serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            try
            {
                // Bind the socket to the port
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(HOST), PORT);
                serverSocket.Bind(localEndPoint);

                // Listen for incoming connections
                serverSocket.Listen(1);
                serverSocket.Blocking = false;

                Debug.Log($"Server listening on {HOST}:{PORT}");
                Debug.Log("Waiting for Kinect client to connect...");
            }
            catch (SocketException se)
            {
                Debug.LogError($"Failed to bind socket: {se.Message}");
            }

        }

        // ----------------------------------
        // SOCKET IMPLEMENTATION ADDED HERE |
        // ----------------------------------
        void TryAcceptClient()
        {
            try
            {
                // Non-blocking accept
                clientSocket = serverSocket.Accept();
                clientSocket.Blocking = false;

                IPEndPoint remoteEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
                Debug.Log($"Connected to client: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
            }
            catch (SocketException se)
            {
                // WouldBlock means no connection waiting, which is fine
                if (se.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogWarning($"Accept error: {se.Message}");
                }
            }
        }

        
        void ReceiveData()
        {
            try
            {
                int bytesRead = clientSocket.Receive(receiveBuffer, 0, BUFFER_SIZE, SocketFlags.None);

                if (bytesRead > 0)
                {
                    // Decode received data
                    string data = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    buffer += data;

                    // Process complete JSON objects (delimited by newline)
                    ProcessBuffer();
                }
            }
            catch (SocketException se)
            {
                // WouldBlock means no data available, which is fine for non-blocking
                if (se.SocketErrorCode != SocketError.WouldBlock)
                {
                    Debug.LogWarning($"Receive error: {se.Message}");
                    clientSocket = null;
                }
            }
        }

        void ProcessBuffer()
        {
            while (buffer.Contains("\n"))
            {
                int newlineIndex = buffer.IndexOf('\n');
                string line = buffer.Substring(0, newlineIndex);
                buffer = buffer.Substring(newlineIndex + 1);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        // Debug.Log($"Received Data : {line.Clone()}...");
                        // Parse JSON
                        SkeletonData skeletonData = JsonUtility.FromJson<SkeletonData>(line);
                        var pelvisData = skeletonData.joints[0];
                        // Debug.Log($"MINE READER PELVIS Position (x, y, z): {pelvisData.position.x}, {pelvisData.position.y}, {pelvisData.position.z}");
                        // Debug.Log($"MINE READER PELVIS Orientation (x, y, z, w): {pelvisData.orientation.x}, {pelvisData.orientation.y}, {pelvisData.orientation.z}, {pelvisData.orientation.w}");
                        // Debug.Log($"MINE READER Type Quaternion: {pelvisData.orientation.GetType()}");
                        frameCount++;

                        // Print summary
                        
                        int numJoints = skeletonData.joints != null ? skeletonData.joints.Count : 0;
                        // Debug.Log($"Frame {frameCount}: Body ID={skeletonData.body_id}, Timestamp={skeletonData.timestamp}, Joints={numJoints}");

                        if (skeletonData.joints != null)
                        {
                            // Create Joint array (Skeleton expects 32 joints in order)
                            var joints = new K4AdotNet.BodyTracking.Joint[32];

                            // Use reflection to create Skeleton since constructor is internal
                            var skeletonType = typeof(K4AdotNet.BodyTracking.Skeleton);
                            var skeleton = (K4AdotNet.BodyTracking.Skeleton)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(skeletonType);
                            
                            foreach (var jd in skeletonData.joints)
                            {
                                int jointIndex = (int) jd.joint;
                                
                                skeleton[jointIndex] = new K4AdotNet.BodyTracking.Joint
                                {
                                    PositionMm = new Float3(jd.position.x, jd.position.y, jd.position.z),
                                    Orientation = new K4AdotNet.Quaternion(jd.orientation.w, jd.orientation.x, jd.orientation.y, jd.orientation.z),
                                    ConfidenceLevel =(JointConfidenceLevel) jd.confidence_level
                                };
                            }

                            SkeletonUpdated?.Invoke(this, new SkeletonEventArgs(skeleton));
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse JSON: {e.Message}");
                        Debug.Log($"Data: {line.Substring(0, Mathf.Min(100, line.Length))}...");
                    }
                }
            }
        }


        // --------------------------------------
        // UPDATED DUE TO SOCKET IMPLEMENTATION |
        // --------------------------------------
        private void OnDestroy()
        {
            IsAvailable = false;

            // Clean up sockets
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }

            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }

            Debug.Log($"Total frames received: {frameCount}");
        }

        private void Update()
        {
            // If no client connected, try to accept
            if (clientSocket == null)
            {
                IsAvailable = false;
                IsInitializationComplete = false;
                TryAcceptClient();
                return;
            }

            // If client connected, try to receive data
            if (clientSocket != null && clientSocket.Connected)
            {
                IsAvailable = true;
                IsInitializationComplete = true;
                ReceiveData();
            }
            else
            {
                Debug.Log("Client disconnected");
                clientSocket = null;
                IsAvailable = false;
                IsInitializationComplete = false;
            }
        }
    }
}
