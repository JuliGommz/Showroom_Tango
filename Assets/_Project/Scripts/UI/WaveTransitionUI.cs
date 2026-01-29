/*
====================================================================
* WaveTransitionUI - Wave Transition Countdown Display
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-22
* Version: 1.2
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Requirement for wave transition feedback
* - Countdown duration matching wave delay
* - Text-only approach
* 
* [AI-ASSISTED]
* - Event-driven wave cleared detection (EnemySpawner.OnWaveCleared)
* - Countdown coroutine implementation
* - UI text updates
* 
* [AI-GENERATED]
* - Complete implementation
* 
* DEPENDENCIES:
* - TMPro (TextMeshPro)
* - EnemySpawner (OnWaveCleared event)
* 
* NOTES:
* - Subscribes to EnemySpawner.OnWaveCleared event (fires when all enemies dead)
* - Displays "NEXT WAVE IN X..." countdown for 3 seconds
* - Only shows between waves, not after final wave
====================================================================
*/

using System.Collections;
using UnityEngine;
using TMPro;

public class WaveTransitionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Settings")]
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private Color textColor = new Color(0.667f, 0f, 0.784f, 1f);

    void Start()
    {
        if (countdownPanel == null)
        {
            Debug.LogError("[WaveTransitionUI] countdownPanel not assigned in Inspector!");
            enabled = false;
            return;
        }

        if (countdownText == null)
        {
            Debug.LogError("[WaveTransitionUI] countdownText not assigned in Inspector!");
            enabled = false;
            return;
        }

        countdownPanel.SetActive(false);
        countdownText.color = textColor;
    }

    void OnEnable()
    {
        EnemySpawner.OnWaveCleared += HandleWaveCleared;
    }

    void OnDisable()
    {
        EnemySpawner.OnWaveCleared -= HandleWaveCleared;
    }

    private void HandleWaveCleared(int clearedWave)
    {
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        int maxWaves = spawner != null ? spawner.GetMaxWaves() : 3;

        // Only show countdown if not final wave
        if (clearedWave < maxWaves)
        {
            StartCoroutine(ShowCountdown(clearedWave + 1));
        }
    }

    private IEnumerator ShowCountdown(int nextWave)
    {
        countdownPanel.SetActive(true);

        for (int i = countdownSeconds; i > 0; i--)
        {
            countdownText.text = $"NEXT WAVE IN {i}...";
            yield return new WaitForSeconds(1f);
        }

        countdownPanel.SetActive(false);
    }
}
