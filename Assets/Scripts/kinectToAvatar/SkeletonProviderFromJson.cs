using K4AdotNet.BodyTracking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace K4AdotNet.Samples.Unity
{
    /// <summary>
    /// Provides skeleton data from a JSON file for static pose display.
    /// This allows multiple avatars to load different poses from different files.
    /// Uses the shared SkeletonData structures from SkeletonData.cs
    /// </summary>
    public class SkeletonProviderFromJson : MonoBehaviour
    {
        [Header("JSON File Settings")]
        [Tooltip("Name of the JSON file in the Poses folder (e.g., 'frontal_lunge_arms_up.json')")]
        public string jsonFileName = "frontal_lunge_arms_up.json";

        [Tooltip("Load and apply the pose automatically on Start")]
        public bool loadOnStart = true;

        // Event triggered when skeleton data is loaded
        public event EventHandler<SkeletonEventArgs> SkeletonUpdated;

        private Skeleton? _currentSkeleton;
        private bool _isLoaded = false;

        void Start()
        {
            if (loadOnStart)
            {
                LoadPoseFromJson();
            }
        }

        /// <summary>
        /// Load pose data from the specified JSON file
        /// </summary>
        public void LoadPoseFromJson()
        {
            LoadPoseFromJson(jsonFileName);
        }

        /// <summary>
        /// Load pose data from a specific JSON file
        /// </summary>
        /// <param name="fileName">Name of the JSON file in the Poses folder</param>
        public void LoadPoseFromJson(string fileName)
        {
            try
            {
                // Use StreamingAssets folder which works on all platforms including Android/Quest
                string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Poses", fileName);
                
                Debug.Log($"[SkeletonProviderFromJson] Attempting to load from: {filePath}");

                string jsonContent = null;

                // On Android, StreamingAssets are inside the APK and need special handling
                #if UNITY_ANDROID && !UNITY_EDITOR
                UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
                www.SendWebRequest();
                
                // Wait for the request to complete
                while (!www.isDone) { }
                
                if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[SkeletonProviderFromJson] JSON file not found: {filePath}\nError: {www.error}");
                    return;
                }
                
                jsonContent = www.downloadHandler.text;
                #else
                // On other platforms (Editor, Standalone), use File.ReadAllText
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogError($"[SkeletonProviderFromJson] JSON file not found: {filePath}");
                    return;
                }
                
                jsonContent = System.IO.File.ReadAllText(filePath);
                #endif
                
                // Parse the JSON using SkeletonData from SkeletonData.cs
                SkeletonData skeletonData = JsonUtility.FromJson<SkeletonData>(jsonContent);

                if (skeletonData == null || skeletonData.joints == null)
                {
                    Debug.LogError($"[SkeletonProviderFromJson] Failed to parse skeleton data from: {fileName}");
                    return;
                }

                Debug.Log($"[SkeletonProviderFromJson] Loaded JSON file: {fileName}");
                Debug.Log($"[SkeletonProviderFromJson] Parsed {skeletonData.joints.Count} joints from JSON");

                // Convert to K4AdotNet Skeleton structure
                Skeleton skeleton = ConvertToSkeleton(skeletonData);
                _currentSkeleton = skeleton;
                _isLoaded = true;

                // Trigger the event to notify CharacterAnimator
                OnSkeletonUpdated(skeleton);

                Debug.Log($"[SkeletonProviderFromJson] Successfully loaded and applied pose from: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkeletonProviderFromJson] Error loading JSON file '{fileName}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Convert SkeletonData to K4AdotNet Skeleton structure
        /// </summary>
        private Skeleton ConvertToSkeleton(SkeletonData data)
        {
            Skeleton skeleton = new Skeleton();

            // Iterate through all joints in the SkeletonData
            foreach (var jd in data.joints)
            {
                int jointIndex = (int) jd.joint;
                                
                skeleton[jointIndex] = new K4AdotNet.BodyTracking.Joint
                {
                    PositionMm = new Float3(jd.position.x, jd.position.y, jd.position.z),
                    Orientation = new K4AdotNet.Quaternion(jd.orientation.w, jd.orientation.x, jd.orientation.y, jd.orientation.z),
                    ConfidenceLevel =(JointConfidenceLevel) jd.confidence_level
                };
            }

            return skeleton;
        }

        /// <summary>
        /// Trigger the SkeletonUpdated event
        /// </summary>
        private void OnSkeletonUpdated(Skeleton skeleton)
        {
            SkeletonUpdated?.Invoke(this, new SkeletonEventArgs(skeleton));
        }

        /// <summary>
        /// Clear the current skeleton and hide the avatar
        /// </summary>
        public void ClearPose()
        {
            _currentSkeleton = null;
            _isLoaded = false;
            SkeletonUpdated?.Invoke(this, new SkeletonEventArgs(null));
            Debug.Log("[SkeletonProviderFromJson] Pose cleared");
        }

        /// <summary>
        /// Check if a pose is currently loaded
        /// </summary>
        public bool IsPoseLoaded => _isLoaded;

        /// <summary>
        /// Get the current skeleton
        /// </summary>
        public Skeleton? CurrentSkeleton => _currentSkeleton;
    }
}
