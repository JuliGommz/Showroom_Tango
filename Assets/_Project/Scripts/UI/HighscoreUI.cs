/*
====================================================================
* HighscoreUI.cs - Top 10 Highscore Leaderboard Display
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design SRH Hochschule
* Developer: Julian Gomez
* Date: 2025-01-27
* Version: 1.0
*
* WICHTIG: KOMMENTIERUNG NICHT LOESCHEN!
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

        // Build formatted text for the leaderboard
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>--- TOP 10 HIGHSCORES ---</b>");
        sb.AppendLine();

        for (int i = 0; i < entries.Count && i < 10; i++)
        {
            string rank = (i + 1).ToString().PadLeft(2);
            string name = entries[i].playerName;
            string score = entries[i].score.ToString("N0");

            // Truncate long names
            if (name.Length > 15) name = name.Substring(0, 15) + "..";

            sb.AppendLine($"{rank}.  {name.PadRight(18)} {score.PadLeft(8)}");
        }

        DisplayFallbackText(sb.ToString());
        Debug.Log($"[HighscoreUI] Displayed {entries.Count} highscore entries");
    }

    private void DisplayFallbackText(string text)
    {
        if (highscoreListText != null)
        {
            highscoreListText.text = text;
        }
    }
}
