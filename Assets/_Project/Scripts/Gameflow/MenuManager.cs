/*
====================================================================
* MenuManager - Main Menu Controller
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 1.2
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Menu layout (Story popup, Preferences)
* - Scene flow (Menu -> Game)
* 
* [AI-ASSISTED]
* - Scene loading logic
* - Popup management
* 
* [AI-GENERATED]
* - Complete implementation structure
* 
* DEPENDENCIES:
* - UnityEngine.SceneManagement
* - PreferencesMenu (settings popup)
* 
* NOTES:
* - Start Game button loads Game scene directly
* - Game scene contains both Lobby and Gameplay
* - Single-scene architecture
====================================================================
*/

using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject storyPopup;
    [SerializeField] private PreferencesMenu preferencesMenu;

    public void StartGame()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void ShowStoryPopup()
    {
        if (storyPopup != null)
        {
            storyPopup.SetActive(true);
        }
    }

    public void ShowPreferencesPopup()
    {
        if (preferencesMenu != null)
        {
            preferencesMenu.ShowMenu();
        }
    }

    public void ClosePopup(GameObject popup)
    {
        if (popup != null)
        {
            popup.SetActive(false);
        }
    }
}
