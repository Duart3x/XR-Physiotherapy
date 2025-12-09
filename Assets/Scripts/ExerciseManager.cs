using UnityEngine;
using K4AdotNet.BodyTracking;
using System.Collections.Generic;
using System.Linq;

namespace K4AdotNet.Samples.Unity
{
    using UnityQuaternion = UnityEngine.Quaternion;
    using K4AQuaternion = K4AdotNet.Quaternion;

    public class ExerciseManager : MonoBehaviour
    {
        public SkeletonProvider userSkeletonProvider;
        public SkeletonProviderFromJson targetSkeletonProvider;
        public Transform alignmentRoot; // Reference frame for drawing
        
        [Header("Visualization Settings")]
        public float arrowWidth = 0.02f;
        public Color arrowColor = Color.cyan;
        public Material lineMaterial;
        
        // Internal state
        private Skeleton? userSkeleton;
        private Skeleton? targetSkeleton;
        private Dictionary<JointType, GameObject> arrows = new Dictionary<JointType, GameObject>();

        private void Start()
        {
            if (userSkeletonProvider == null)
                userSkeletonProvider = FindObjectOfType<SkeletonProvider>();
                
            if (targetSkeletonProvider == null)
                targetSkeletonProvider = FindObjectOfType<SkeletonProviderFromJson>();
            
            if (alignmentRoot == null && userSkeletonProvider != null)
                alignmentRoot = userSkeletonProvider.transform.parent;

            if (userSkeletonProvider != null)
                userSkeletonProvider.SkeletonUpdated += (s, e) => userSkeleton = e.Skeleton;
            
            if (targetSkeletonProvider != null)
                targetSkeletonProvider.SkeletonUpdated += (s, e) => targetSkeleton = e.Skeleton;

            // Create arrows for key joints
            CreateArrow(JointType.WristLeft);
            CreateArrow(JointType.ElbowLeft);
            CreateArrow(JointType.WristRight);
            CreateArrow(JointType.ElbowRight);
            CreateArrow(JointType.KneeLeft);
            CreateArrow(JointType.AnkleLeft);
            CreateArrow(JointType.KneeRight);
            CreateArrow(JointType.AnkleRight);
        }

        private void CreateArrow(JointType joint)
        {
            GameObject arrow = new GameObject($"Arrow_{joint}");
            arrow.transform.SetParent(transform);
            LineRenderer lr = arrow.AddComponent<LineRenderer>();
            lr.startWidth = arrowWidth;
            lr.endWidth = 0.005f; // Tapered end
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            
            if (lineMaterial != null)
                lr.material = lineMaterial;
            else
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) lr.material = new Material(shader);
            }
            lr.material.color = arrowColor;
            
            arrows[joint] = arrow;
            arrow.SetActive(false);
        }

        private void Update()
        {
            if (userSkeleton.HasValue && targetSkeleton.HasValue)
            {
                UpdateArrows(userSkeleton.Value, targetSkeleton.Value);
            }
        }

        private void UpdateArrows(Skeleton user, Skeleton target)
        {
            // Assumption: Root (Pelvis) is the anchor.
            var userRootPos = ConvertPos(user[JointType.Pelvis].PositionMm);
            var userRootRot = ConvertRot(user[JointType.Pelvis].Orientation);
            
            var targetRootRot = ConvertRot(target[JointType.Pelvis].Orientation);
            
            foreach (var kvp in arrows)
            {
                JointType joint = kvp.Key;
                GameObject arrowObj = kvp.Value;
                LineRenderer lr = arrowObj.GetComponent<LineRenderer>();
                
                Vector3 userJointPos = ConvertPos(user[joint].PositionMm);
                Vector3 idealPos = CalculateIdealPosition(joint, user, target, userRootPos, userRootRot, targetRootRot);
                
                // Transform to World Space
                if (alignmentRoot != null)
                {
                    userJointPos = alignmentRoot.TransformPoint(userJointPos);
                    idealPos = alignmentRoot.TransformPoint(idealPos);
                }

                if (Vector3.Distance(userJointPos, idealPos) > 0.05f) // Threshold
                {
                    arrowObj.SetActive(true);
                    lr.SetPosition(0, userJointPos);
                    lr.SetPosition(1, idealPos);
                    lr.startColor = arrowColor;
                    lr.endColor = arrowColor;
                }
                else
                {
                    arrowObj.SetActive(false);
                }
            }
        }

        private Vector3 CalculateIdealPosition(JointType joint, Skeleton user, Skeleton target, Vector3 userRootPos, UnityQuaternion userRootRot, UnityQuaternion targetRootRot)
        {
            List<JointType> path = new List<JointType>();
            JointType current = joint;
            while (current != JointType.Pelvis)
            {
                path.Add(current);
                current = GetParent(current);
                if (current == JointType.Pelvis) break;
            }
            path.Reverse();
            
            Vector3 currentPos = userRootPos;
            UnityQuaternion parentDesiredRot = userRootRot;
            Vector3 parentPos = userRootPos;
            JointType parentType = JointType.Pelvis;

            foreach (var node in path)
            {
                Vector3 targetVec = ConvertPos(target[node].PositionMm) - ConvertPos(target[parentType].PositionMm);
                UnityQuaternion targetParentGlobal = ConvertRot(target[parentType].Orientation);
                Vector3 targetVecLocal = UnityQuaternion.Inverse(targetParentGlobal) * targetVec;
                
                UnityQuaternion targetNodeRot = ConvertRot(target[node].Orientation);
                UnityQuaternion targetLocalRot = UnityQuaternion.Inverse(targetParentGlobal) * targetNodeRot;
                UnityQuaternion desiredRot = parentDesiredRot * targetLocalRot;
                
                Vector3 desiredVecGlobal = parentDesiredRot * targetVecLocal;
                
                Vector3 userParentPos = ConvertPos(user[parentType].PositionMm);
                Vector3 userNodePos = ConvertPos(user[node].PositionMm);
                float boneLength = Vector3.Distance(userParentPos, userNodePos);
                
                currentPos = parentPos + desiredVecGlobal.normalized * boneLength;
                
                parentPos = currentPos;
                parentDesiredRot = desiredRot;
                parentType = node;
            }
            
            return currentPos;
        }

        private JointType GetParent(JointType joint)
        {
            switch (joint)
            {
                case JointType.SpineNavel: return JointType.Pelvis;
                case JointType.SpineChest: return JointType.SpineNavel;
                case JointType.Neck: return JointType.SpineChest;
                case JointType.Head: return JointType.Neck;
                
                case JointType.ClavicleLeft: return JointType.SpineChest;
                case JointType.ShoulderLeft: return JointType.ClavicleLeft;
                case JointType.ElbowLeft: return JointType.ShoulderLeft;
                case JointType.WristLeft: return JointType.ElbowLeft;
                case JointType.HandLeft: return JointType.WristLeft;
                case JointType.HandTipLeft: return JointType.HandLeft;
                case JointType.ThumbLeft: return JointType.WristLeft;

                case JointType.ClavicleRight: return JointType.SpineChest;
                case JointType.ShoulderRight: return JointType.ClavicleRight;
                case JointType.ElbowRight: return JointType.ShoulderRight;
                case JointType.WristRight: return JointType.ElbowRight;
                case JointType.HandRight: return JointType.WristRight;
                case JointType.HandTipRight: return JointType.HandRight;
                case JointType.ThumbRight: return JointType.WristRight;

                case JointType.HipLeft: return JointType.Pelvis;
                case JointType.KneeLeft: return JointType.HipLeft;
                case JointType.AnkleLeft: return JointType.KneeLeft;
                case JointType.FootLeft: return JointType.AnkleLeft;

                case JointType.HipRight: return JointType.Pelvis;
                case JointType.KneeRight: return JointType.HipRight;
                case JointType.AnkleRight: return JointType.KneeRight;
                case JointType.FootRight: return JointType.AnkleRight;
                
                case JointType.Nose: return JointType.Head;
                case JointType.EyeLeft: return JointType.Head;
                case JointType.EarLeft: return JointType.Head;
                case JointType.EyeRight: return JointType.Head;
                case JointType.EarRight: return JointType.Head;

                default: return JointType.Pelvis;
            }
        }

        private Vector3 ConvertPos(Float3 p) => new Vector3(p.X, -p.Y, p.Z) * 0.001f;
        private UnityQuaternion ConvertRot(K4AQuaternion q) => UnityQuaternion.AngleAxis(180, Vector3.up) * new UnityQuaternion(q.X, q.Y, q.Z, q.W);
    }
}