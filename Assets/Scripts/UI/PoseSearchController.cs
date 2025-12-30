using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Oculus.Interaction.Samples;
using Oculus.Interaction.Body.Input;

namespace Physical.Therapy.UI
{
    /// <summary>
    /// Handles search bar input and filters the displayed pose cells.
    /// Connect this to the Meta SearchBar's InputField OnValueChanged event.
    /// </summary>
    public class PoseSearchController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The PoseService component that provides the list of available poses")]
        public PoseService poseService;

        [Tooltip("Parent transform containing all pose cell GameObjects")]
        public Transform poseCellContainer;

        [Tooltip("Prefab for individual pose cells (optional - can create cells dynamically)")]
        public GameObject poseCellPrefab;

        [Header("Search Settings")]
        [Tooltip("The search input field (optional - for clearing search text)")]
        public TMP_InputField searchInputField;

        [Tooltip("Minimum characters before filtering (0 = filter immediately)")]
        public int minSearchLength = 0;

        [Tooltip("Case-sensitive search")]
        public bool caseSensitive = false;

        [Header("Filter References")]
        [Tooltip("Default difficulty toggle to reset to when clearing filters")]
        public Toggle defaultDifficultyToggle;

        [Tooltip("Default body area toggle to reset to when clearing filters")]
        public Toggle defaultBodyAreaToggle;

        /// <summary>
        /// Event fired when a pose is selected. Returns the pose name.
        /// </summary>
        public event Action<string> OnPoseSelected;

        // Internal tracking of pose cells
        private Dictionary<string, GameObject> poseCells = new Dictionary<string, GameObject>();
        private string currentSearchQuery = "";
        private Difficulty currentDifficulty = Difficulty.None;
        private BodyArea currentBodyArea = BodyArea.None;


        private void Start()
        {
            Debug.Log("[PoseSearchController] Started");

            if (poseService == null)
                poseService = FindObjectOfType<PoseService>();

            if (poseService != null)
            {
                Debug.Log("[PoseSearchController] Found PoseService, subscribing to events");
                poseService.OnPosesScanned += OnPosesScanned;

                // If poses are already scanned, create cells
                if (poseService.AvailablePoses.Count > 0)
                {
                    Debug.Log($"[PoseSearchController] Poses already available: {poseService.AvailablePoses.Count}");
                    OnPosesScanned(poseService.AvailablePoses);
                }
            }
            else
            {
                Debug.LogError("[PoseSearchController] No PoseService found in scene! Add PoseService component.");
            }

            if (poseCellContainer == null)
            {
                Debug.LogError("[PoseSearchController] poseCellContainer is not assigned! Assign it in the Inspector.");
            }
            else
            {
                Debug.Log($"[PoseSearchController] Cell container: {poseCellContainer.name}");
            }
        }

        private void OnDestroy()
        {
            if (poseService != null)
            {
                poseService.OnPosesScanned -= OnPosesScanned;
            }
        }

        /// <summary>
        /// Called when PoseService finishes scanning. Creates cells for each pose.
        /// </summary>
        private void OnPosesScanned(List<PoseMetadata> poses)
        {
            Debug.Log($"[PoseSearchController] Creating cells for {poses.Count} poses");
            CreatePoseCells(poses);


            ApplySearchAndFilters();
        }

        /// <summary>
        /// Creates UI cells for each pose. Override this if you need custom cell creation.
        /// </summary>
        protected virtual void CreatePoseCells(List<PoseMetadata> poses)
        {
            if (poseCellContainer == null)
            {
                Debug.LogError("[PoseSearchController] poseCellContainer is not assigned!");
                return;
            }

            foreach (PoseMetadata pose in poses)
            {
                GameObject cell = CreatePoseCell(pose);
                if (cell != null)
                {
                    poseCells[pose.PoseName] = cell;
                }
            }
        }

        /// <summary>
        /// Creates a single pose cell. Override for custom cell creation.
        /// </summary>
        protected virtual GameObject CreatePoseCell(PoseMetadata pose)
        {
            GameObject cell;

            if (poseCellPrefab != null)
            {
                // Instantiate from prefab
                cell = Instantiate(poseCellPrefab, poseCellContainer);
            }
            else
            {
                // Create a basic button cell
                cell = CreateDefaultCell();
            }

            cell.name = $"PoseCell_{pose.PoseName}";

            // Set up the cell's display text
            SetCellText(cell, PoseService.GetDisplayName(pose.PoseName));

            // Set up the cell's icon sprite
            SetCellIcon(cell, pose);

            // Set up click handler
            SetCellClickHandler(cell, pose.PoseName);

            return cell;
        }

        /// <summary>
        /// Called when a pose cell is clicked.
        /// Fires the OnPoseSelected event - subscribe to this event from the K4AdotNet layer to load poses.
        /// </summary>
        public void SelectPose(string poseName)
        {
            Debug.Log($"[PoseSearchController] Pose selected: {poseName}");

            // Fire event for external listeners (e.g., PoseLoadBridge in K4AdotNet namespace)
            OnPoseSelected?.Invoke(poseName);
        }

        /// <summary>
        /// Creates a default cell if no prefab is assigned
        /// </summary>
        private GameObject CreateDefaultCell()
        {
            GameObject cell = new GameObject("PoseCell", typeof(RectTransform));
            cell.transform.SetParent(poseCellContainer, false);

            // Add Image for background
            Image bg = cell.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add Button component
            Button button = cell.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
            button.colors = colors;

            // Set size
            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 60);

            // Add text child
            GameObject textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(cell.transform, false);

            // Try to use TextMeshPro if available, otherwise use legacy Text
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.color = Color.white;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 5);
            textRt.offsetMax = new Vector2(-10, -5);

            return cell;
        }

        /// <summary>
        /// Sets the display text on a cell.
        /// Searches for text components in multiple ways to support various prefab structures.
        /// </summary>
        private void SetCellText(GameObject cell, string text)
        {
            // Try to find text component by common label names first
            string[] labelNames = { "Label", "Text", "Title", "ButtonText", "TMPLabel", "TextMeshPro" };

            foreach (string labelName in labelNames)
            {
                Transform labelTransform = cell.transform.Find(labelName);
                if (labelTransform != null)
                {
                    // Try TMP
                    TextMeshProUGUI tmp = labelTransform.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = text;
                        Debug.Log($"[PoseSearchController] Set text '{text}' on {labelName} (TMP)");
                        return;
                    }

                    // Try legacy Text
                    Text legacyText = labelTransform.GetComponent<Text>();
                    if (legacyText != null)
                    {
                        legacyText.text = text;
                        Debug.Log($"[PoseSearchController] Set text '{text}' on {labelName} (Legacy)");
                        return;
                    }
                }
            }

            // Fallback: search all children for TextMeshProUGUI
            TextMeshProUGUI[] tmpComponents = cell.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmpComponents.Length > 0)
            {
                // Use the first one found (usually the main label)
                tmpComponents[0].text = text;
                Debug.Log($"[PoseSearchController] Set text '{text}' on first TMP found: {tmpComponents[0].gameObject.name}");
                return;
            }

            // Fallback: search all children for legacy Text
            Text[] textComponents = cell.GetComponentsInChildren<Text>(true);
            if (textComponents.Length > 0)
            {
                textComponents[0].text = text;
                Debug.Log($"[PoseSearchController] Set text '{text}' on first Text found: {textComponents[0].gameObject.name}");
                return;
            }

            Debug.LogWarning($"[PoseSearchController] Could not find text component on cell '{cell.name}' to set text '{text}'");
        }

        /// <summary>
        /// Sets the icon sprite on a cell.
        /// Searches for Image components by common icon names to support various prefab structures.
        /// </summary>
        private void SetCellIcon(GameObject cell, PoseMetadata pose)
        {
            if (poseService == null) return;

            Sprite iconSprite = poseService.GetIconSprite(pose);
            if (iconSprite == null)
            {
                Debug.Log($"[PoseSearchController] No icon sprite for pose '{pose.PoseName}'");
                return;
            }

            // Try to find icon Image component by common names
            string[] iconNames = { "Icon", "Image", "Thumbnail", "PoseIcon", "IconImage", "Sprite" };

            foreach (string iconName in iconNames)
            {
                Transform iconTransform = cell.transform.Find(iconName);
                if (iconTransform != null)
                {
                    Image image = iconTransform.GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = iconSprite;
                        Debug.Log($"[PoseSearchController] Set icon sprite for '{pose.PoseName}' on {iconName} with sprite '{iconSprite.name}'");
                        return;
                    }
                }
            }

            // Fallback: search all children for Image components (skip the background)
            Image[] images = cell.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                // Skip if this is likely the background (same GameObject as cell or named "Background")
                if (image.gameObject == cell) continue;
                if (image.gameObject.name.ToLower().Contains("background")) continue;
                if (image.gameObject.name.ToLower().Contains("bg")) continue;

                // Found a candidate image component
                image.sprite = iconSprite;
                Debug.Log($"[PoseSearchController] Set icon sprite for '{pose.PoseName}' on {image.gameObject.name} with sprite '{iconSprite.name}' (fallback)");
                return;
            }

            Debug.LogWarning($"[PoseSearchController] Could not find icon Image component on cell '{cell.name}'");
        }

        /// <summary>
        /// Sets up the click handler for a cell
        /// </summary>
        private void SetCellClickHandler(GameObject cell, string poseName)
        {
            Button button = cell.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectPose(poseName));
                return;
            }

            // Try Toggle
            Toggle toggle = cell.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn) SelectPose(poseName);
                });
            }
        }

        # region Search

        /// <summary>
        /// Call this from the SearchBar's InputField OnValueChanged event.
        /// This is the main search callback method.
        /// </summary>
        public void OnSearchValueChanged(string searchQuery)
        {
            currentSearchQuery = searchQuery ?? "";
            ApplySearchAndFilters();
        }

        # endregion Search

        # region Body Area & Difficulty Filter Callbacks

        public void OnDifficultyFilterChanged(int difficultyIndex)
        {
            Debug.Log($"[PoseSearchController] Difficulty filter changed: {difficultyIndex}");
            currentDifficulty = (Difficulty) difficultyIndex;
            ApplySearchAndFilters();
        }

        public void OnBodyAreaChanged(int bodyAreaIndex)
        {
            Debug.Log($"[PoseSearchController] Body area filter changed: {bodyAreaIndex}");
            currentBodyArea = (BodyArea) (bodyAreaIndex == 0 ? bodyAreaIndex : 1 << bodyAreaIndex);
            ApplySearchAndFilters();
        }

        # endregion Body Area & Difficulty Filter Callbacks

        # region Apply Search And Filters

        /// <summary>
        /// Filters visible poses based on search query and filters
        /// </summary>
        public void ApplySearchAndFilters()
        {
            Debug.Log($"[PoseSearchController] ApplySearchAndFilters: {{searchQuery='{currentSearchQuery}', difficulty={currentDifficulty}, bodyArea={currentBodyArea}}}");
            string searchTerm = currentSearchQuery.Trim();

            // If search is too short, show all
            bool showAll = searchTerm.Length < minSearchLength;

            foreach (var kvp in poseCells)
            {
                string poseName = kvp.Key;
                GameObject cell = kvp.Value;

                if (cell == null) continue;

                bool shouldShow;
                if (showAll || string.IsNullOrEmpty(searchTerm))
                {
                    shouldShow = true;
                }
                else
                {
                    // Check if pose name contains search query
                    string nameToSearch = caseSensitive ? poseName : poseName.ToLower();
                    string termToFind = caseSensitive ? searchTerm : searchTerm.ToLower();

                    // Also check display name
                    string displayName = PoseService.GetDisplayName(poseName);
                    string displayToSearch = caseSensitive ? displayName : displayName.ToLower();

                    shouldShow = nameToSearch.Contains(termToFind) || displayToSearch.Contains(termToFind);
                }

                if (shouldShow && currentDifficulty != Difficulty.None)
                {
                    PoseMetadata metadata = poseService.GetPoseByName(poseName);
                    if (metadata != null)
                    {
                        shouldShow = (metadata.Difficulty == currentDifficulty);
                    }
                }

                if (shouldShow && currentBodyArea != BodyArea.None)
                {
                    PoseMetadata metadata = poseService.GetPoseByName(poseName);
                    if (metadata != null)
                    {
                        shouldShow = metadata.TargetsBodyArea(currentBodyArea);
                    }
                }

                cell.SetActive(shouldShow);
            }
        }

        # endregion Apply Search And Filters

        # region Clear Everything

        /// <summary>
        /// Clears the search input and resets all filters
        /// </summary>
        public void OnClearSearchAndFilters()
        {
            // Clear search input
            if (searchInputField != null)
            {
                searchInputField.text = "";
            }
            currentSearchQuery = "";

            currentDifficulty = Difficulty.None;
            defaultDifficultyToggle.isOn = true;

            currentBodyArea = BodyArea.None;
            defaultBodyAreaToggle.isOn = true;

            ApplySearchAndFilters();
        }

        # endregion Clear Everything

    }
}
