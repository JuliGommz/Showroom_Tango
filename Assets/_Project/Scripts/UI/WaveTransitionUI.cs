/*
====================================================================
* WaveTransitionUI - Wave Transition Countdown Display
====================================================================
* Project: Showroom_Tango (2-Player Top-Down Bullet-Hell)
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-22
* Version: 1.1 - Event-driven wave detection (replaces polling)
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

    private EnemySpawner enemySpawner;

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
        StartCoroutine(SubscribeWhenReady());
    }

    void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveCleared -= HandleWaveCleared;
        }
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Wait for EnemySpawner to exist
        while (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
            if (enemySpawner != null)
            {
                enemySpawner.OnWaveCleared += HandleWaveCleared;
                Debug.Log("[WaveTransitionUI] Subscribed to EnemySpawner.OnWaveCleared");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void HandleWaveCleared(int clearedWave)
    {
        if (enemySpawner == null) return;

        // Only show countdown if there are more waves
        if (clearedWave < enemySpawner.GetMaxWaves())
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
