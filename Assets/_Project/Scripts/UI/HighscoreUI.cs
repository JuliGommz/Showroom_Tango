/*
====================================================================
* HighscoreUI - Top 10 Highscore Leaderboard Display
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-27
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Top 10 leaderboard requirement
* - Display on GameOver/Victory panels
* 
* [AI-ASSISTED]
* - UnityWebRequest callback integration
* - Dynamic UI row generation
* 
* [AI-GENERATED]
* - Complete UI display logic
* 
* DEPENDENCIES:
* - TMPro (TextMeshPro)
* - HighscoreManager (backend integration)
* 
* SETUP:
* 1. Attach to a GameObject inside GameOver/Victory panel
* 2. Assign highscoreListText (a TextMeshProUGUI element)
====================================================================
*/

using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HighscoreUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI highscoreListText;

    [Header("Settings")]
    [SerializeField] private bool loadOnEnable = true;

    void OnEnable()
    {
        if (loadOnEnable)
        {
            RefreshHighscores();
        }
    }

    public void RefreshHighscores()
    {
        if (HighscoreManager.Instance == null)
        {
            Debug.LogWarning("[HighscoreUI] HighscoreManager not found");
            DisplayFallbackText("Highscores unavailable");
            return;
        }

        DisplayFallbackText("Loading...");
        HighscoreManager.Instance.LoadHighscores(OnHighscoresLoaded);
    }

    private void OnHighscoresLoaded(List<HighscoreEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            DisplayFallbackText("No highscores yet");
            return;
        }

        // Format leaderboard display
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- TOP 10 HIGHSCORES ---");
        sb.AppendLine();

        for (int i = 0; i < entries.Count && i < 10; i++)
        {
            string rank = (i + 1).ToString().PadLeft(2);
            string name = entries[i].playerName;
            string score = entries[i].score.ToString("N0");

            // Truncate long names for display
            if (name.Length > 15) name = name.Substring(0, 15) + "..";

            sb.AppendLine($"{rank}. {name.PadRight(18)} {score.PadLeft(8)}");
        }

        DisplayFallbackText(sb.ToString());
    }

    private void DisplayFallbackText(string text)
    {
        if (highscoreListText != null)
        {
            highscoreListText.text = text;
        }
    }
}
