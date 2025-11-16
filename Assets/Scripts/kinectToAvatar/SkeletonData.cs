using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class JointData
{
    // The kinect joint id (int) will translate to the JointType Skeleton enum (from package)
    // Reference: https://github.com/bibigone/k4a.net/blob/master/K4AdotNet/BodyTracking/Skeleton.cs
    public K4AdotNet.BodyTracking.JointType joint;
    public Vector3 position;
    public UnityEngine.Quaternion orientation;
    public int confidence_level;
}


[Serializable]
public class SkeletonData
{
    public int body_id;
    public long timestamp;
    public List<JointData> joints;
}