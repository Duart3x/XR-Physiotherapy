using K4AdotNet.BodyTracking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace K4AdotNet.Samples.Unity
{
    public class SkeletonRenderer : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("[SkeletonRenderer] Awake");
            _root = new GameObject();
            _root.name = "skeleton:root";
            _root.transform.parent = transform;
            _root.transform.localScale = Vector3.one;
            _root.transform.localPosition = Vector3.zero;
            _root.SetActive(false);

            // Find a safe shader for URP/Built-in that supports color
            _shader = Shader.Find("Sprites/Default");
            if (_shader == null) _shader = Shader.Find("Universal Render Pipeline/Unlit");

            CreateJoints();
            CreateBones();
            CreateHead();
        }

        #region Render objects

        private GameObject _root;
        private IReadOnlyDictionary<JointType, Transform> _joints;
        private IReadOnlyCollection<Bone> _bones;
        private Transform _head;
        private Shader _shader;

        private class Bone
        {
            public static Bone FromChildJoint(JointType childJoint, Shader shader)
            {
                return new Bone(childJoint.GetParent(), childJoint, shader);
            }

            public Bone(JointType parentJoint, JointType childJoint, Shader shader)
            {
                ParentJoint = parentJoint;
                ChildJoint = childJoint;

                var pos = new GameObject();
                pos.name = $"{parentJoint}->{childJoint}:pos";

                var bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bone.name = $"{parentJoint}->{childJoint}:bone";
                bone.transform.parent = pos.transform;
                bone.transform.localScale = new Vector3(0.033f, 0.5f, 0.033f);
                bone.transform.localPosition = 0.5f * Vector3.up;

                if (shader != null)
                {
                    bone.GetComponent<Renderer>().material = new Material(shader);
                }

                Transform = pos.transform;
            }

            public JointType ParentJoint { get; }
            public JointType ChildJoint { get; }
            public Transform Transform { get; }
        }

        private void CreateJoints()
        {
            // Joints are rendered as spheres
            _joints = JointTypes.All
                .ToDictionary(
                    jt => jt,
                    jt =>
                    {
                        var joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        joint.name = jt.ToString();
                        joint.transform.parent = _root.transform;
                        joint.transform.localScale = 0.075f * Vector3.one;
                        
                        if (_shader != null)
                        {
                            joint.GetComponent<Renderer>().material = new Material(_shader);
                        }

                        return joint.transform;
                    });

            // Set green as default color
            SetJointColor(Color.green, typeof(JointType).GetEnumValues().Cast<JointType>().ToArray());

            // Set slightly decreased size for some joints
            SetJointScale(0.05f, JointType.Neck, JointType.Head, JointType.ClavicleLeft, JointType.ClavicleRight, JointType.EarLeft, JointType.EarRight);

            // Set greatly decreased size and specific colors for face joints
            SetJointScale(0.033f, JointType.EyeLeft, JointType.EyeRight, JointType.Nose);
            SetJointColor(Color.cyan, JointType.EyeLeft, JointType.EyeRight);
            SetJointColor(Color.magenta, JointType.Nose);
            SetJointColor(Color.yellow, JointType.EarLeft, JointType.EarRight);
        }

        private void SetJointScale(float scale, params JointType[] jointTypes)
        {
            foreach (var jt in jointTypes)
                _joints[jt].localScale = scale * Vector3.one;
        }

        private void SetJointColor(Color color, params JointType[] jointTypes)
        {
            foreach (var jt in jointTypes)
                _joints[jt].GetComponent<Renderer>().material.color = color;
        }

        private void CreateBones()
        {
            var bones = new List<Bone>();

            // Spine
            CreateBones(bones, JointType.SpineNavel, JointType.SpineChest, JointType.Neck, JointType.Head);
            // Right arm
            CreateBones(bones, JointType.ClavicleRight, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight);
            // Left arm
            CreateBones(bones, JointType.ClavicleLeft, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft);
            // Right leg
            CreateBones(bones, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight);
            // Left leg
            CreateBones(bones, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft);

            _bones = bones;
            foreach (var b in _bones)
            {
                b.Transform.parent = _root.transform;
            }
        }

        private void CreateBones(ICollection<Bone> list, params JointType[] childJoints)
        {
            foreach (var joint in childJoints)
            {
                list.Add(Bone.FromChildJoint(joint, _shader));
            }
        }

        private void CreateHead()
        {
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (_shader != null)
            {
                head.GetComponent<Renderer>().material = new Material(_shader);
            }
            head.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 0.8f);
            head.transform.parent = _root.transform;

            _head = head.transform;
        }

        #endregion

        private void OnEnable()
        {
            // Find the single SkeletonProvider in the scene (for live tracking)
            var skeletonProvider = GetComponentInParent<SkeletonProvider>();
            
            if (skeletonProvider != null)
            {
                skeletonProvider.SkeletonUpdated += SkeletonProvider_SkeletonUpdated;
                Debug.Log($"[SkeletonRenderer] {gameObject.name} - Subscribed to SkeletonProvider on {skeletonProvider.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[SkeletonRenderer] {gameObject.name} - No SkeletonProvider found in scene!");
            }
        }

        private void OnDisable()
        {
            var skeletonProvider = GetComponentInParent<SkeletonProvider>();
            if (skeletonProvider != null)
            {
                skeletonProvider.SkeletonUpdated -= SkeletonProvider_SkeletonUpdated;
                Debug.Log($"[SkeletonRenderer] {gameObject.name} - Unsubscribed from SkeletonProvider");
            }
        }

        private void SkeletonProvider_SkeletonUpdated(object sender, SkeletonEventArgs e)
        {
            if (e.Skeleton == null)
            {
                HideSkeleton();
            }
            else
            {
                RenderSkeleton(e.Skeleton.Value);
            }
        }

        private void RenderSkeleton(Skeleton skeleton)
        {
            // Calculate offset to anchor the skeleton to the Pelvis (making Pelvis local (0,0,0))
            // This prevents the skeleton from moving around the space when the user walks,
            // keeping it aligned ("directly above") the Avatar.
            var offset = ConvertKinectPos(skeleton[JointType.Pelvis].PositionMm);

            foreach (var item in _joints)
            {
                item.Value.localPosition = ConvertKinectPos(skeleton[item.Key].PositionMm) - offset;
            }

            foreach (var bone in _bones)
            {
                PositionBone(bone, skeleton, offset);
            }

            PositionHead(skeleton, offset);

            _root.SetActive(true);
        }

        private static void PositionBone(Bone bone, Skeleton skeleton, Vector3 offset)
        {
            var parentPos = ConvertKinectPos(skeleton[bone.ParentJoint].PositionMm) - offset;
            var direction = (ConvertKinectPos(skeleton[bone.ChildJoint].PositionMm) - offset) - parentPos;
            bone.Transform.localPosition = parentPos;
            bone.Transform.localScale = new Vector3(1, direction.magnitude, 1);
            bone.Transform.localRotation = UnityEngine.Quaternion.FromToRotation(Vector3.up, direction);
        }

        private void PositionHead(Skeleton skeleton, Vector3 offset)
        {
            var headPos = ConvertKinectPos(skeleton[JointType.Head].PositionMm) - offset;
            var earPosR = ConvertKinectPos(skeleton[JointType.EarRight].PositionMm) - offset;
            var earPosL = ConvertKinectPos(skeleton[JointType.EarLeft].PositionMm) - offset;
            var headCenter = 0.5f * (earPosR + earPosL);
            var d = (earPosR - earPosL).magnitude;
            _head.localPosition = headCenter;
            _head.localRotation = UnityEngine.Quaternion.FromToRotation(Vector3.up, headCenter - headPos);
            _head.localScale = new Vector3(d, 2 * (headCenter - headPos).magnitude, d);
        }

        private static Vector3 ConvertKinectPos(Float3 pos)
        {
            // Kinect Y axis points down, so negate Y coordinate
            // Scale to convert millimeters to meters
            // https://docs.microsoft.com/en-us/azure/Kinect-dk/coordinate-systems
            // Other transforms (positioning of the skeleton in the scene, mirroring)
            // are handled by properties of ascendant GameObject's
            return 0.001f * new Vector3(pos.X, -pos.Y, pos.Z);
        }

        public void SetJointColor(JointType joint, Color color)
        {
            if (_joints != null && _joints.ContainsKey(joint))
            {
                var renderer = _joints[joint].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        public void SetBoneColor(JointType childJoint, Color color)
        {
            if (_bones != null)
            {
                var bone = _bones.FirstOrDefault(b => b.ChildJoint == childJoint);
                if (bone != null)
                {
                    // The bone geometry (Cylinder) is the child of the bone Transform
                    var renderer = bone.Transform.GetChild(0).GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
        }

        private void HideSkeleton()
        {
            _root.SetActive(false);
        }
    }
}