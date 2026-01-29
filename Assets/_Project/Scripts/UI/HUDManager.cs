/*
====================================================================
* HUDManager - In-Game HUD Display Controller
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-20
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - HUD layout concept (HP bars top left/right, score center)
* - Game Over/Victory screen requirements
* 
* [AI-ASSISTED]
* - Unity UI integration
* - Event subscription pattern
* - Dynamic player HP tracking
* 
* [AI-GENERATED]
* - Complete UI update logic
* 
* DEPENDENCIES:
* - TMPro (TextMeshPro)
* - UnityEngine.UI (Slider)
* - ScoreManager (team/player scores)
* - GameStateManager (game state events)
* - HighscoreManager (leaderboard submission)
* 
* NOTES:
* - Updates player HP 5x per second via InvokeRepeating
* - Updates wave display every second
* - Event-driven score updates
* - Handles game state transitions (Playing -> GameOver/Victory)
====================================================================
*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Wave Display")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Player 1 HUD")]
    [SerializeField] private Slider player1HPBar;
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player1ScoreText;

    [Header("Player 2 HUD")]
    [SerializeField] private Slider player2HPBar;
    [SerializeField] private TextMeshProUGUI player2NameText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;

    [Header("Game Over/Victory")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private UnityEngine.UI.Button restartButton;

    void Start()
    {
        // Subscribe to managers
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            UpdateScore(ScoreManager.Instance.GetTeamScore());
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged += HandleGameStateChange;
        }

        // Hide end screens
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        // Validate restart button configuration (diagnostic only)
        if (restartButton != null)
        {
            int listenerCount = restartButton.onClick.GetPersistentEventCount();
            if (listenerCount == 0)
            {
                Debug.LogWarning("[HUDManager] Restart button has ZERO onClick listeners! Configure in Inspector!");
            }
        }
        else
        {
            Debug.LogWarning("[HUDManager] Restart button reference not assigned in Inspector!");
        }

        // Start periodic updates
        InvokeRepeating(nameof(UpdatePlayerHP), 0.5f, 0.2f);
        InvokeRepeating(nameof(UpdateWave), 1f, 1f);
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChange;
        }
    }

    private void UpdateScore(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {newScore}";
        }
    }

    private void UpdateWave()
    {
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[HUDManager] EnemySpawner not found for wave display");
            return;
        }

        if (waveText == null)
        {
            Debug.LogWarning("[HUDManager] waveText not assigned in Inspector!");
            return;
        }

        waveText.text = $"WAVE {spawner.GetCurrentWave()} / {spawner.GetMaxWaves()}";
    }

    private void UpdatePlayerHP()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length >= 1)
        {
            UpdateSinglePlayerHP(players[0], player1HPBar, player1NameText, player1ScoreText);
        }
        else
        {
            // Clear player 1 display when absent
            if (player1ScoreText != null) player1ScoreText.text = "Score: 0";
        }

        if (players.Length >= 2)
        {
            UpdateSinglePlayerHP(players[1], player2HPBar, player2NameText, player2ScoreText);
        }
        else
        {
            // Clear player 2 display when absent
            if (player2ScoreText != null) player2ScoreText.text = "Score: 0";
        }
    }

    private void UpdateSinglePlayerHP(GameObject playerObj, Slider hpBar, TextMeshProUGUI nameText, TextMeshProUGUI scoreText)
    {
        if (hpBar == null) return;

        PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
        PlayerController controller = playerObj.GetComponent<PlayerController>();

        // Update individual score
        if (scoreText != null && ScoreManager.Instance != null)
        {
            int individualScore = ScoreManager.Instance.GetPlayerScore(playerObj);
            scoreText.text = $"Score: {individualScore}";
        }

        if (health != null)
        {
            int currentHP = health.GetCurrentHealth();
            int maxHP = health.GetMaxHealth();
            float hpRatio = (float)currentHP / maxHP;

            hpBar.value = hpRatio;

            if (nameText != null && controller != null)
            {
                nameText.text = controller.GetPlayerName();
            }
        }
        else
        {
            Debug.LogWarning($"[HUDManager] No PlayerHealth on {playerObj.name}");
        }
    }

    private void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Lobby:
            case GameState.Playing:
                // Hide all end screens when returning to lobby or playing
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                if (victoryPanel != null) victoryPanel.SetActive(false);
                break;

            case GameState.GameOver:
                ShowGameOver();
                break;

            case GameState.Victory:
                ShowVictory();
                break;
        }
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            UpdateFinalScore();
            SubmitHighscores();
        }
    }

    private void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            UpdateFinalScore();
            SubmitHighscores();
        }
    }

    private void SubmitHighscores()
    {
        if (HighscoreManager.Instance == null || ScoreManager.Instance == null) return;

        int teamScore = ScoreManager.Instance.GetTeamScore();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            // Fallback when no player objects remain (all dead)
            HighscoreManager.Instance.SubmitScore("Team", teamScore);
            return;
        }

        foreach (GameObject playerObj in players)
        {
            PlayerController controller = playerObj.GetComponent<PlayerController>();
            string playerName = controller != null ? controller.GetPlayerName() : "Player";
            int individualScore = ScoreManager.Instance.GetPlayerScore(playerObj);

            HighscoreManager.Instance.SubmitScore(playerName, individualScore > 0 ? individualScore : teamScore);
        }
    }

    private void UpdateFinalScore()
    {
        if (finalScoreText != null && ScoreManager.Instance != null)
        {
            finalScoreText.text = $"FINAL SCORE: {ScoreManager.Instance.GetTeamScore()}";
        }
    }

    /// <summary>
    /// Called by Restart button in UI (connected via Inspector)
    /// </summary>
    public void OnRestartButtonPressed()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("[HUDManager] 🔴 RESTART BUTTON CLICKED!");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RequestRestartServerRpc();
        }
        else
        {
            Debug.LogError("[HUDManager] GameStateManager.Instance is NULL! Cannot restart!");

            // Attempt manual recovery
            GameStateManager found = FindAnyObjectByType<GameStateManager>();
            if (found != null)
            {
                Debug.LogWarning($"[HUDManager] Found GameStateManager manually: {found.name}, but Instance was null!");
            }
            else
            {
                Debug.LogError("[HUDManager] GameStateManager doesn't exist in scene at all!");
            }
        }
    }
}
