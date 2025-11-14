using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class JointData
{
    public KinectJoint joint;
    public Vector3 position;
    public Quaternion orientation;
    public int confidence_level;
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
    private Transform _rootJointTransform;
    private Animator animator;
    private class JointDataInfo
    {
        public JointDataInfo(KinectJoint joint, Transform transform, Quaternion tposeOrientation, Quaternion kinectTPoseOrientationInverse)
        {
            Transform = transform;
            TPoseOrientation = tposeOrientation;
            KinectTPoseOrientationInverse = kinectTPoseOrientationInverse;
        }

        public Transform Transform { get; }
        public Quaternion TPoseOrientation { get; }
        public Quaternion KinectTPoseOrientationInverse { get; }

        public override string ToString()
        {
            return $"JointDataInfo(Transform={Transform.name}, TPoseOrientation={TPoseOrientation}, KinectTPoseOrientationInverse={KinectTPoseOrientationInverse})";
        }
    }

    private Dictionary<KinectJoint, JointDataInfo> _joints;

    // Avatar GameObject to sync
    public GameObject avatar;

    // Buffer for incoming data
    private string buffer = "";
    private int frameCount = 0;

    // For non-blocking receives
    private byte[] receiveBuffer = new byte[BUFFER_SIZE];

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

        animator = avatar.GetComponent<Animator>();
        Debug.Log($"Animator: {animator}");

        GetJointDataInfoForAll();
    }


    void GetJointDataInfoForAll()
    {
        // Set avatar rotation to identity to avoid T-pose orientation issues
        avatar.transform.rotation = Quaternion.identity;

        // Get root joint (PELVIS) and set it as the the center of the coordinate space
        _rootJointTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
        _rootJointTransform.localPosition = Vector3.zero;
        
        _joints = KinectJointHelper.GetAllJoints().ToDictionary(j => j, j => GetJointDataInfo(j, animator));
    }

    private JointDataInfo GetJointDataInfo(KinectJoint joint, Animator animator)
    {
        Debug.Log("Processing -----------------------------------");
        Debug.Log("Processing -----------------------------------");
        var hbb = ConvertFromKinectJoint(joint);
        if (hbb == HumanBodyBones.LastBone)
            return null;

        Debug.Log($"Processing joint: {joint}");

        var transform = animator.GetBoneTransform(hbb);
        Debug.Log($"Processing transform: {transform.name} {transform.rotation}");
        if (transform == null)
        {
            Debug.LogWarning($"No transform found for joint: {joint}");
            return null;
        }


        var tPoseBone = GetSkeletonBone(animator, transform);
        Debug.Log($"Processing skeletonBone: {tPoseBone.name} {tPoseBone.rotation}");

        var tposeOrientation = tPoseBone.rotation;

        // Loop
        Debug.Log("Processing Starting at transform: " + transform.name);
        var t = transform;
        while (!ReferenceEquals(t, _rootJointTransform))
        {
            Debug.Log("Processing Traversing to parent: " + t.parent.name);
            t = t.parent;
            tposeOrientation = GetSkeletonBone(animator, t).rotation * tposeOrientation;
        }

        var kinectTPoseOrientationInverse = GetKinectTPoseOrientationInverse(joint);
        Debug.Log($"Processing kinectTPoseOrientationInverse for joint {joint}: {kinectTPoseOrientationInverse}");

        var jointDataInfo = new JointDataInfo(joint, transform, tposeOrientation, kinectTPoseOrientationInverse);
        Debug.Log($"Created JointDataInfo for joint {joint}: {jointDataInfo}");

        return jointDataInfo;
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
                    Debug.Log($"Received Data : {line.Clone()}...");
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
                            Debug.Log($" {joint.joint} position: x={joint.position.x:F2}, y={joint.position.y:F2}, z={joint.position.z:F2}");
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

        Debug.Log($"Applying skeleton data to avatar...");

        var characterPos = ConvertFromKinectPosition(skeletonData.joints.Find(j => j.joint == KinectJoint.PELVIS).position);
        avatar.transform.position = characterPos;

        foreach (JointData joint in skeletonData.joints)
        {

            Debug.Log("Applying --------------------------------------------------");
            Debug.Log("Applying --------------------------------------------------");
            Debug.Log($"Applying joint: {joint.joint}");

            var jointReferenceData = _joints[joint.joint];
            if (jointReferenceData == null)
            {
                Debug.LogWarning($"Applying No joint reference data for joint: {joint.joint}");
                continue;
            }

            var orientation = ConvertFromKinectQuaternion(joint.orientation);
            var rotationRel2TPoseInCharacterSpace = orientation * jointReferenceData.KinectTPoseOrientationInverse;
            var rotationInCharacterSpace = rotationRel2TPoseInCharacterSpace * jointReferenceData.TPoseOrientation;
            var invParentRotationInCharacterSpace = Quaternion.identity;
            var t = jointReferenceData.Transform;
            while (!ReferenceEquals(t, _rootJointTransform))
            {
                t = t.parent;
                invParentRotationInCharacterSpace *= Quaternion.Inverse(t.localRotation);
            }
            jointReferenceData.Transform.localRotation = invParentRotationInCharacterSpace * rotationInCharacterSpace;
        }
    }

    private static HumanBodyBones ConvertFromKinectJoint(KinectJoint kinectJoint)
    {
        switch (kinectJoint)
        {
            case KinectJoint.PELVIS: return HumanBodyBones.Hips;
            case KinectJoint.SPINE_NAVEL: return HumanBodyBones.Spine;
            case KinectJoint.SPINE_CHEST: return HumanBodyBones.Chest;
            case KinectJoint.NECK: return HumanBodyBones.Neck;
            case KinectJoint.HEAD: return HumanBodyBones.Head;
            case KinectJoint.CLAVICLE_LEFT: return HumanBodyBones.LeftShoulder;
            case KinectJoint.SHOULDER_LEFT: return HumanBodyBones.LeftUpperArm;
            case KinectJoint.ELBOW_LEFT: return HumanBodyBones.LeftLowerArm;
            case KinectJoint.WRIST_LEFT: return HumanBodyBones.LeftHand;
            case KinectJoint.CLAVICLE_RIGHT: return HumanBodyBones.RightShoulder;
            case KinectJoint.SHOULDER_RIGHT: return HumanBodyBones.RightUpperArm;
            case KinectJoint.ELBOW_RIGHT: return HumanBodyBones.RightLowerArm;
            case KinectJoint.WRIST_RIGHT: return HumanBodyBones.RightHand;
            case KinectJoint.HIP_LEFT: return HumanBodyBones.LeftUpperLeg;
            case KinectJoint.KNEE_LEFT: return HumanBodyBones.LeftLowerLeg;
            case KinectJoint.ANKLE_LEFT: return HumanBodyBones.LeftFoot;
            case KinectJoint.FOOT_LEFT: return HumanBodyBones.LeftToes;
            case KinectJoint.HIP_RIGHT: return HumanBodyBones.RightUpperLeg;
            case KinectJoint.KNEE_RIGHT: return HumanBodyBones.RightLowerLeg;
            case KinectJoint.ANKLE_RIGHT: return HumanBodyBones.RightFoot;
            case KinectJoint.FOOT_RIGHT: return HumanBodyBones.RightToes;
            default: return HumanBodyBones.LastBone; // Invalid
        }
    }

    private static SkeletonBone GetSkeletonBone(Animator animator, Transform transform)
    {
        return animator.avatar.humanDescription.skeleton.First(skeletonBone => skeletonBone.name == transform.name);
    }

    private static Vector3 ConvertFromKinectPosition(Vector3 pos)
    {
        // Kinect Y axis points down, so negate Y coordinate
        // Scale to convert millimeters to meters
        // https://docs.microsoft.com/en-us/azure/Kinect-dk/coordinate-systems
        // Other transforms (positioning of the skeleton in the scene, mirroring)
        // are handled by properties of ascendant GameObject's
        return 0.001f * new Vector3(pos.x, -pos.y, pos.z);
    }

    private static Quaternion ConvertFromKinectQuaternion(Quaternion q)
    {
        // Kinect coordinate system for rotations seems to be
        // left-handed Y+ up, Z+ towards camera
        // So apply 180 rotation around Y to align with Unity coords (Z away from camera)
        return Quaternion.AngleAxis(180, Vector3.up) * new Quaternion(q.x, q.y, q.z, q.w);
    }

    private static Quaternion GetKinectTPoseOrientationInverse(KinectJoint kinectJoint)
        {
            // Used this page as reference for T-pose orientations
            // https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints
            // Assuming T-pose as body facing Z+, with head at Y+. Same for target character
            // Coordinate system seems to be left-handed not right handed as depicted
            // Thus inverse T-pose rotation should align Y and Z axes of depicted local coords for a joint with body coords in T-pose
            switch (kinectJoint)
            {
                case KinectJoint.PELVIS:
                case KinectJoint.SPINE_NAVEL:
                case KinectJoint.SPINE_CHEST:
                case KinectJoint.NECK:
                case KinectJoint.HEAD:
                case KinectJoint.HIP_LEFT:
                case KinectJoint.KNEE_LEFT:
                case KinectJoint.ANKLE_LEFT:
                    return Quaternion.AngleAxis(90, Vector3.forward) * Quaternion.AngleAxis(-90, Vector3.up);

                case KinectJoint.FOOT_LEFT:
                    return Quaternion.AngleAxis(-90, Vector3.up);

                case KinectJoint.HIP_RIGHT:
                case KinectJoint.KNEE_RIGHT:
                case KinectJoint.ANKLE_RIGHT:
                    return Quaternion.AngleAxis(-90, Vector3.forward) * Quaternion.AngleAxis(-90, Vector3.up);

                case KinectJoint.FOOT_RIGHT:
                    return Quaternion.AngleAxis(180, Vector3.forward) * Quaternion.AngleAxis(-90, Vector3.up);

                case KinectJoint.CLAVICLE_LEFT:
                case KinectJoint.SHOULDER_LEFT:
                case KinectJoint.ELBOW_LEFT:
                    return Quaternion.AngleAxis(90, Vector3.right);

                case KinectJoint.WRIST_LEFT:
                    return Quaternion.AngleAxis(180, Vector3.right);

                case KinectJoint.CLAVICLE_RIGHT:
                case KinectJoint.SHOULDER_RIGHT:
                case KinectJoint.ELBOW_RIGHT:
                    return Quaternion.AngleAxis(-90, Vector3.right);

                case KinectJoint.WRIST_RIGHT:
                    return Quaternion.identity;

                default:
                    Debug.LogWarning($"Unknown joint type for T-pose orientation: {kinectJoint}");
                    return Quaternion.identity;
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
