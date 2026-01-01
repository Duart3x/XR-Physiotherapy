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
    /// Loads pose metadata from manifest.txt including body areas, difficulty, and icon references.
    /// </summary>
    public class PoseService : MonoBehaviour
    {
        [Header("Folder Paths Streaming Assets")]
        [Tooltip("Subfolder within StreamingAssets containing pose json files")]
        public string posesJsonFolder = "Poses";

        [Tooltip("Subfolder within Resources containing pose icon sprites")]
        public string iconsResourceFolder = "Icons";

        [Header("Settings")]
        [Tooltip("Scan for poses automatically on Start")]
        public bool scanOnStart = true;

        /// <summary>
        /// Event fired when pose scanning is complete. Returns list of pose metadata.
        /// </summary>
        public event Action<List<PoseMetadata>> OnPosesScanned;

        /// <summary>
        /// List of discovered poses with metadata
        /// </summary>
        public List<PoseMetadata> AvailablePoses { get; private set; } = new List<PoseMetadata>();

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

            string posesPath = Path.Combine(Application.streamingAssetsPath, posesJsonFolder);
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

            var poseNames = AvailablePoses.ConvertAll(p => p.PoseName);
            Debug.Log($"[PoseService] Found {AvailablePoses.Count} poses: {string.Join(", ", poseNames)}");
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

            // Try to load from manifest first
            string manifestPath = Path.Combine(posesPath, "manifest.txt");
            if (File.Exists(manifestPath))
            {
                Debug.Log($"[PoseService] Loading poses from manifest: {manifestPath}");
                string[] lines = File.ReadAllLines(manifestPath);
                ParseManifestLines(lines);
            }
            else
            {
                // Fallback: scan for .json files and create basic metadata
                Debug.Log($"[PoseService] No manifest found, scanning for .json files");
                string[] jsonFiles = Directory.GetFiles(posesPath, "*.json");
                foreach (string filePath in jsonFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    AvailablePoses.Add(new PoseMetadata(fileName, BodyArea.None, Difficulty.Easy));
                }
            }

            AvailablePoses.Sort((a, b) => string.Compare(a.PoseName, b.PoseName, StringComparison.OrdinalIgnoreCase));
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
                    // Parse manifest file with metadata
                    string[] lines = www.downloadHandler.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    ParseManifestLines(lines);
                }
                else
                {
                    Debug.LogWarning($"[PoseService] No manifest.txt found, trying known poses...");
                    yield return StartCoroutine(TryLoadKnownPoses(posesPath));
                }
            }

            AvailablePoses.Sort((a, b) => string.Compare(a.PoseName, b.PoseName, StringComparison.OrdinalIgnoreCase));
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
                        AvailablePoses.Add(new PoseMetadata(poseName, BodyArea.None, Difficulty.Easy));
                    }
                }
            }
        }

        /// <summary>
        /// Parse manifest lines into PoseMetadata objects.
        /// Format: filename.json | icon1 | icon2 | body_areas | difficulty | description | flip_rotation
        /// </summary>
        private void ParseManifestLines(string[] lines)
        {
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                // Split by pipe delimiter
                string[] parts = trimmedLine.Split('|');

                // Get pose name (required)
                string poseName = parts[0].Trim();
                if (poseName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    poseName = poseName.Substring(0, poseName.Length - 5);
                }

                if (string.IsNullOrEmpty(poseName))
                    continue;

                // Parse based on number of parts
                // Format: filename.json | icon1 | icon2 | body_areas | difficulty | description | flip_rotation
                string iconFileName = "";
                string secondIconFileName = "";
                BodyArea bodyAreas = BodyArea.None;
                Difficulty difficulty = Difficulty.Easy;
                string description = "";
                bool applyYRotation180 = false;

                if (parts.Length >= 2)
                {
                    iconFileName = parts[1].Trim();
                }
                if (parts.Length >= 3)
                {
                    secondIconFileName = parts[2].Trim();
                }
                if (parts.Length >= 4)
                {
                    bodyAreas = PoseMetadata.ParseBodyAreas(parts[3].Trim());
                }
                if (parts.Length >= 5)
                {
                    difficulty = PoseMetadata.ParseDifficulty(parts[4].Trim());
                }
                if (parts.Length >= 6)
                {
                    description = parts[5].Trim();
                }
                if (parts.Length >= 7)
                {
                    string flipValue = parts[6].Trim().ToLower();
                    applyYRotation180 = flipValue == "true" || flipValue == "1" || flipValue == "yes";
                }

                AvailablePoses.Add(new PoseMetadata(poseName, bodyAreas, difficulty, description, iconFileName, secondIconFileName, applyYRotation180));
                Debug.Log($"[PoseService] Loaded pose: {poseName}, Icon1: {iconFileName}, Icon2: {secondIconFileName}, Areas: {bodyAreas}, Difficulty: {difficulty}, Flip: {applyYRotation180}");
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

        /// <summary>
        /// Load a sprite for a pose's icon from Resources folder.
        /// Sprites should be placed in Assets/Resources/Icons/ (or the configured iconsResourceFolder).
        /// </summary>
        public Sprite GetIconSprite(PoseMetadata pose)
        {
            if (pose == null || string.IsNullOrEmpty(pose.IconFileName))
                return null;

            return LoadSpriteByName(pose.IconFileName);
        }

        /// <summary>
        /// Load a sprite by name from the Resources/Icons folder.
        /// The name should be without file extension.
        /// </summary>
        public Sprite LoadSpriteByName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return null;

            // Remove file extension if present
            string nameWithoutExtension = spriteName;
            int extIndex = spriteName.LastIndexOf('.');
            if (extIndex > 0)
            {
                nameWithoutExtension = spriteName.Substring(0, extIndex);
            }

            // Try loading from the configured icons folder within Resources
            string resourcePath = string.IsNullOrEmpty(iconsResourceFolder)
                ? nameWithoutExtension
                : $"{iconsResourceFolder}/{nameWithoutExtension}";

            Sprite sprite = Resources.Load<Sprite>(resourcePath);

            if (sprite == null)
            {
                Debug.LogWarning($"[PoseService] Could not load sprite: {resourcePath}");
            }

            return sprite;
        }

        /// <summary>
        /// Find a pose by name
        /// </summary>
        public PoseMetadata GetPoseByName(string poseName)
        {
            return AvailablePoses.Find(p =>
                string.Equals(p.PoseName, poseName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
