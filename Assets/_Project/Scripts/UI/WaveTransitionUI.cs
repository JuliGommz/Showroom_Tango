/*
====================================================================
* WaveTransitionUI - Wave Transition Countdown Display
====================================================================
* Project: Showroom_Tango (2-Player Top-Down Bullet-Hell)
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-22
* Version: 1.2 - Static event subscription (fixes instance mismatch bug)
*
* WICHTIG: KOMMENTIERUNG NICHT LOESCHEN!
* Diese detaillierte Authorship-Dokumentation ist fuer die akademische
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
* - Academic header formatting
*
* [AI-GENERATED]
* - None
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
    [SerializeField] private Color textColor = new Color(0.667f, 0f, 0.784f, 1f); // Magenta

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
        // Subscribe to static event - works regardless of which EnemySpawner instance fires it
        EnemySpawner.OnWaveCleared += HandleWaveCleared;
        Debug.Log("[WaveTransitionUI] Subscribed to EnemySpawner.OnWaveCleared (static event)");
    }

    void OnDisable()
    {
        EnemySpawner.OnWaveCleared -= HandleWaveCleared;
        Debug.Log("[WaveTransitionUI] Unsubscribed from EnemySpawner.OnWaveCleared");
    }

    private void HandleWaveCleared(int clearedWave)
    {
        Debug.Log($"[WaveTransitionUI] HandleWaveCleared called! Wave {clearedWave} cleared.");

        // Find EnemySpawner to get max waves (static event doesn't give us instance)
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        int maxWaves = spawner != null ? spawner.GetMaxWaves() : 3; // Default to 3 if not found

        Debug.Log($"[WaveTransitionUI] clearedWave={clearedWave}, maxWaves={maxWaves}");

        // Only show countdown if there are more waves
        if (clearedWave < maxWaves)
        {
            Debug.Log($"[WaveTransitionUI] Starting countdown for wave {clearedWave + 1}");
            StartCoroutine(ShowCountdown(clearedWave + 1));
        }
        else
        {
            Debug.Log("[WaveTransitionUI] Final wave cleared - no countdown needed");
        }
    }

    private IEnumerator ShowCountdown(int nextWave)
    {
        Debug.Log($"[WaveTransitionUI] ShowCountdown started for wave {nextWave}");
        countdownPanel.SetActive(true);

        for (int i = countdownSeconds; i > 0; i--)
        {
            countdownText.text = $"NEXT WAVE IN {i}...";
            Debug.Log($"[WaveTransitionUI] Countdown: {i}");
            yield return new WaitForSeconds(1f);
        }

        countdownPanel.SetActive(false);
        Debug.Log("[WaveTransitionUI] Countdown complete, panel hidden");
    }
}
