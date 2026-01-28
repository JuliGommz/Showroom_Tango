using UnityEngine;
using TMPro;

/// <summary>
/// Story Popup Manager for Showroom Tango
/// Displays lore text when triggered by menu button
/// Author: Julian Gomez
/// Course: PRG - SRH Hochschule Heidelberg
/// Date: January 28, 2026
/// </summary>
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
        // Keep panel active but invisible using CanvasGroup
        popupPanel.SetActive(true);

        // Get or add CanvasGroup component
        canvasGroup = popupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popupPanel.AddComponent<CanvasGroup>();
        }

        // Hide popup without deactivating GameObject
        HidePopup();
    }

    /// <summary>
    /// Call this from Story Button onClick event or MenuManager
    /// </summary>
    public void ShowPopup()
    {
        // Make visible and interactable
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Update text content
        storyText.text = storyLore;

        // Pause game/menu
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Call this from Close Button onClick event
    /// </summary>
    public void ClosePopup()
    {
        HidePopup();

        // Resume game/menu
        Time.timeScale = 1f;
    }

    private void HidePopup()
    {
        // Make invisible and non-interactable
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
