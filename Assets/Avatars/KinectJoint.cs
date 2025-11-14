using System;
using System.Linq;

public enum KinectJoint
{
    // Reference: https://learn.microsoft.com/en-us/previous-versions/azure/kinect-dk/body-joints
    PELVIS = 0,
    SPINE_NAVEL = 1,
    SPINE_CHEST = 2,
    NECK = 3,
    CLAVICLE_LEFT = 4,
    SHOULDER_LEFT = 5,
    ELBOW_LEFT = 6,
    WRIST_LEFT = 7,
    HAND_LEFT = 8,
    HAND_TIP_LEFT = 9,
    THUMB_LEFT = 10,
    CLAVICLE_RIGHT = 11,
    SHOULDER_RIGHT = 12,
    ELBOW_RIGHT = 13,
    WRIST_RIGHT = 14,
    HAND_RIGHT = 15,
    HAND_TIP_RIGHT = 16,
    THUMB_RIGHT = 17,
    HIP_LEFT = 18,
    KNEE_LEFT = 19,
    ANKLE_LEFT = 20,
    FOOT_LEFT = 21,
    HIP_RIGHT = 22,
    KNEE_RIGHT = 23,
    ANKLE_RIGHT = 24,
    FOOT_RIGHT = 25,
    HEAD = 26,
    NOSE = 27,
    EYE_LEFT = 28,
    EAR_LEFT = 29,
    EYE_RIGHT = 30,
    EAR_RIGHT = 31
}

public static class KinectJointHelper
{
    public static KinectJoint[] GetAllJoints() => Enum.GetValues(typeof(KinectJoint)).Cast<KinectJoint>().ToArray();
}