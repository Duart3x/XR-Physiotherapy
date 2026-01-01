using K4AdotNet.Samples.Unity;
using TMPro;
using UnityEngine;

namespace Physical.Therapy.UI
{
    /// <summary>
    /// Menu service for switching between different UI menus.
    /// </summary>
    public class MenuService : MonoBehaviour
    {
        
        [Header("Menu Panels")]
        [Tooltip("Main menu panel.")]
        public GameObject mainMenuPanel;

        [Tooltip("Settings menu panel.")]
        public GameObject settingsMenuPanel;

        private void Start()
        {
            // Show main menu by default
            ShowMainMenu();
        }

        /// <summary>
        /// Shows the main menu panel and hides the settings panel.
        /// </summary>
        public void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            settingsMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Shows the settings menu panel and hides the main panel.
        /// </summary>
        public void ShowSettingsMenu()
        {
            mainMenuPanel.SetActive(false);
            settingsMenuPanel.SetActive(true);
        }

    }
    
}