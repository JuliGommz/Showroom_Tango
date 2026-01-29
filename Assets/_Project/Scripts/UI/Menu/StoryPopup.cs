/*
====================================================================
* StoryPopup - Lore Display Popup Controller
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2026-01-28
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Story popup concept for lore display
* - Time.timeScale pause behavior
* - CanvasGroup visibility approach
* 
* [AI-ASSISTED]
* - CanvasGroup implementation pattern
* - Component initialization structure
* 
* [AI-GENERATED]
* - Complete UI visibility implementation
* 
* DEPENDENCIES:
* - TMPro (TextMeshPro)
* - UnityEngine.UI (CanvasGroup)
* 
* NOTES:
* - Uses CanvasGroup for smooth show/hide without GameObject deactivation
* - Pauses game with Time.timeScale during display
* - Story content editable in Inspector via TextArea
====================================================================
*/

using UnityEngine;
using TMPro;

public class StoryPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private GameObject closeButton;

    [Header("Story Content - Edit in Inspector")]
    [SerializeField, TextArea(10, 20)]
    private string storyLore =
        "[Placeholder] Enter your story text here in the Inspector.\n\n" +
        "Multiple paragraphs supported.";

    private CanvasGroup canvasGroup;

    void Start()
    {
        // CanvasGroup allows visibility control without GameObject activation
        popupPanel.SetActive(true);

        canvasGroup = popupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popupPanel.AddComponent<CanvasGroup>();
        }

        HidePopup();
    }

    public void ShowPopup()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        storyText.text = storyLore;

        Time.timeScale = 0f;
    }

    public void ClosePopup()
    {
        HidePopup();
        Time.timeScale = 1f;
    }

    private void HidePopup()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
