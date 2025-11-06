using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JointData
{
    public string joint_name;
    public PositionData position;
    public OrientationData orientation;
    public int confidence_level;
}

[Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class OrientationData
{
    public float w;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class SkeletonData
{
    public int body_id;
    public long timestamp;
    public List<JointData> joints;
}

public class SyncPoseWithServer : MonoBehaviour
{
    private Socket serverSocket;
    private Socket clientSocket;
    private const int BUFFER_SIZE = 4096;
    private const string HOST = "127.0.0.1";
    private const int PORT = 8888;

    // Avatar GameObject to sync
    public GameObject avatar;

    // Buffer for incoming data
    private string buffer = "";
    private int frameCount = 0;

    // For non-blocking receives
    private byte[] receiveBuffer = new byte[BUFFER_SIZE];

    // Bone mapping cache
    private Dictionary<string, Transform> boneCache = new Dictionary<string, Transform>();

    // Mapping from Azure Kinect joint names to Unity avatar bone paths
    private Dictionary<string, string> jointToAvatarBoneMap = new Dictionary<string, string>()
    {
        // Spine and Core
        { "PELVIS", "Bip01/Bip01 Pelvis" },
        { "SPINE_NAVEL", "Bip01/Bip01 Pelvis/Bip01 Spine" },
        { "SPINE_CHEST", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2" },
        { "NECK", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck" },
      
        // Head
        { "HEAD", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 Head" },
        { "NOSE", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 Head/Bip01 MNose" },
        { "EYE_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 Head/Bip01 LEye" },
        { "EYE_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 Head/Bip01 REye" },
        { "EAR_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 Head/Bip01 LOuterEyebrow" }, // Approximation
        { "EAR_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 Head/Bip01 ROuterEyebrow" }, // Approximation
  
        // Left Arm
        { "CLAVICLE_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle" },
        { "SHOULDER_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm" },
        { "ELBOW_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm" },
        { "WRIST_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm/Bip01 L Hand" },
        //{ "HAND_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm/Bip01 L Hand" },
        { "HANDTIP_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm/Bip01 L Hand/Bip01 L Finger2" }, // Middle finger tip approximation
        { "THUMB_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Bip01 L Forearm/Bip01 L Hand/Bip01 L Finger0" },
        
        // Right Arm
        { "CLAVICLE_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle" },
        { "SHOULDER_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm" },
        { "ELBOW_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm/Bip01 R Forearm" },
        { "WRIST_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm/Bip01 R Forearm/Bip01 R Hand" },
        //{ "HAND_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm/Bip01 R Forearm/Bip01 R Hand" },
        { "HANDTIP_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm/Bip01 R Forearm/Bip01 R Hand/Bip01 R Finger2" }, // Middle finger tip approximation
        { "THUMB_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm/Bip01 R Forearm/Bip01 R Hand/Bip01 R Finger0" },
        
        // Left Leg
        { "HIP_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 L Thigh" },
        { "KNEE_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 L Thigh/Bip01 L Calf" },
        { "ANKLE_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 L Thigh/Bip01 L Calf/Bip01 L Foot" },
        { "FOOT_LEFT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 L Thigh/Bip01 L Calf/Bip01 L Foot/Bip01 L Toe0" },
     
        // Right Leg
        { "HIP_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 R Thigh" },
        { "KNEE_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 R Thigh/Bip01 R Calf" },
        { "ANKLE_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 R Thigh/Bip01 R Calf/Bip01 R Foot" },
        { "FOOT_RIGHT", "Bip01/Bip01 Pelvis/Bip01 Spine/Bip01 R Thigh/Bip01 R Calf/Bip01 R Foot/Bip01 R Toe0" }
    };

    void Start()
    {
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
            return;
        }

        // Initialize bone cache
        InitializeBoneCache();
    }

    void InitializeBoneCache()
    {
        if (avatar == null)
        {
            Debug.LogWarning("Avatar not assigned!");
            return;
        }

        Debug.Log("Initializing bone cache...");
        int foundBones = 0;
        int missingBones = 0;

        foreach (var kvp in jointToAvatarBoneMap)
        {
            Transform bone = avatar.transform.Find(kvp.Value);
            if (bone != null)
            {
                boneCache[kvp.Key] = bone;
                foundBones++;
                Debug.Log($"✓ Mapped {kvp.Key} -> {kvp.Value}");
            }
            else
            {
                missingBones++;
                Debug.LogWarning($"✗ Bone not found: {kvp.Value} for joint {kvp.Key}");
            }
        }

        Debug.Log($"Bone cache initialized: {foundBones} bones found, {missingBones} bones missing");
    }

    void Update()
    {
        // If no client connected, try to accept
        if (clientSocket == null)
        {
            TryAcceptClient();
            return;
        }

        // If client connected, try to receive data
        if (clientSocket != null && clientSocket.Connected)
        {
            ReceiveData();
        }
        else
        {
            Debug.Log("Client disconnected");
            clientSocket = null;
        }
    }

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
                    // Parse JSON
                    SkeletonData skeletonData = JsonUtility.FromJson<SkeletonData>(line);
                    frameCount++;

                    // Print summary
                    int numJoints = skeletonData.joints != null ? skeletonData.joints.Count : 0;
                    Debug.Log($"Frame {frameCount}: Body ID={skeletonData.body_id}, Timestamp={skeletonData.timestamp}, Joints={numJoints}");

                    // Optional: Print specific joint positions (e.g., head)
                    if (skeletonData.joints != null)
                    {
                        foreach (JointData joint in skeletonData.joints)
                        {
                            if (joint.joint_name == "HEAD")
                            {
                                PositionData pos = joint.position;
                                Debug.Log($"  HEAD position: x={pos.x:F2}, y={pos.y:F2}, z={pos.z:F2}");
                            }
                        }
                    }

                    // Apply skeleton data to avatar
                    ApplySkeletonToAvatar(skeletonData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse JSON: {e.Message}");
                    Debug.Log($"Data: {line.Substring(0, Mathf.Min(100, line.Length))}...");
                }
            }
        }
    }

    void ApplySkeletonToAvatar(SkeletonData skeletonData)
    {
        if (avatar == null || skeletonData.joints == null)
            return;

        // Update avatar based on skeleton data
        foreach (JointData joint in skeletonData.joints)
        {
            // Check if we have a cached bone for this joint
            if (boneCache.ContainsKey(joint.joint_name))
            {
                Transform bone = boneCache[joint.joint_name];

                if (bone != null && joint.orientation != null)
                {

                    Quaternion unityRotation = new Quaternion(
                        joint.orientation.x,
                        joint.orientation.z,
                        joint.orientation.y,
                        joint.orientation.w
                    );

                    bone.localRotation = unityRotation;
                 }
            }
        }
    }

    void OnDestroy()
    {
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
}
