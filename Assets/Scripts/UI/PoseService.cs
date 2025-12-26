using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Physical.Therapy.UI
{
    /// <summary>
    /// Scans the StreamingAssets/Poses folder for available pose JSON files.
    /// </summary>
    public class PoseService : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Subfolder within StreamingAssets containing pose files")]
        public string posesFolder = "Poses";

        [Tooltip("Scan for poses automatically on Start")]
        public bool scanOnStart = true;

        /// <summary>
        /// Event fired when pose scanning is complete. Returns list of pose names (without .json extension).
        /// </summary>
        public event Action<List<string>> OnPosesScanned;

        /// <summary>
        /// List of discovered pose names (without .json extension)
        /// </summary>
        public List<string> AvailablePoses { get; private set; } = new List<string>();

        /// <summary>
        /// Whether scanning is currently in progress
        /// </summary>
        public bool IsScanning { get; private set; }

        private void Start()
        {
            Debug.Log("[PoseService] Started");
            if (scanOnStart)
            {
                ScanForPoses();
            }
        }

        /// <summary>
        /// Start scanning for pose files
        /// </summary>
        public void ScanForPoses()
        {
            if (IsScanning) return;
            StartCoroutine(ScanForPosesCoroutine());
        }

        private IEnumerator ScanForPosesCoroutine()
        {
            IsScanning = true;
            AvailablePoses.Clear();

            string posesPath = Path.Combine(Application.streamingAssetsPath, posesFolder);
            Debug.Log($"[PoseService] Scanning for poses in: {posesPath}");

#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, we need to use a manifest file or known file list
            // because we can't directly enumerate files in StreamingAssets
            yield return StartCoroutine(ScanAndroidPoses(posesPath));
#else
            // On Editor/Standalone, we can directly enumerate files
            ScanDesktopPoses(posesPath);
            yield return null;
#endif

            Debug.Log($"[PoseService] Found {AvailablePoses.Count} poses: {string.Join(", ", AvailablePoses)}");
            IsScanning = false;
            OnPosesScanned?.Invoke(AvailablePoses);
        }

        private void ScanDesktopPoses(string posesPath)
        {
            if (!Directory.Exists(posesPath))
            {
                Debug.LogWarning($"[PoseService] Poses directory not found: {posesPath}");
                return;
            }

            string[] jsonFiles = Directory.GetFiles(posesPath, "*.json");
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                AvailablePoses.Add(fileName);
            }

            AvailablePoses.Sort();
        }

        private IEnumerator ScanAndroidPoses(string posesPath)
        {
            // On Android, try to load a manifest file that lists all poses
            string manifestPath = Path.Combine(posesPath, "manifest.txt");

            using (UnityWebRequest www = UnityWebRequest.Get(manifestPath))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // Parse manifest file (one pose name per line)
                    string[] lines = www.downloadHandler.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        string poseName = line.Trim();
                        if (!string.IsNullOrEmpty(poseName) && !poseName.StartsWith("#"))
                        {
                            // Remove .json extension if present
                            if (poseName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            {
                                poseName = poseName.Substring(0, poseName.Length - 5);
                            }
                            AvailablePoses.Add(poseName);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[PoseService] No manifest.txt found, trying known poses...");
                    yield return StartCoroutine(TryLoadKnownPoses(posesPath));
                }
            }

            AvailablePoses.Sort();
        }

        private IEnumerator TryLoadKnownPoses(string posesPath)
        {
            // List of known pose files to check (fallback for Android without manifest)
            string[] knownPoses = new string[]
            {
                "frontal_lunge_arms_up",
                "guerreiro",
                "gato",
                "aviao"
            };

            foreach (string poseName in knownPoses)
            {
                string filePath = Path.Combine(posesPath, poseName + ".json");

                using (UnityWebRequest www = UnityWebRequest.Get(filePath))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AvailablePoses.Add(poseName);
                    }
                }
            }
        }

        /// <summary>
        /// Get the full file name for a pose (with .json extension)
        /// </summary>
        public static string GetPoseFileName(string poseName)
        {
            return poseName + ".json";
        }

        /// <summary>
        /// Get a display-friendly name for a pose (replaces underscores with spaces, title case)
        /// </summary>
        public static string GetDisplayName(string poseName)
        {
            if (string.IsNullOrEmpty(poseName)) return "";

            // Replace underscores with spaces
            string display = poseName.Replace('_', ' ');

            // Title case
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(display.ToLower());
        }
    }
}
