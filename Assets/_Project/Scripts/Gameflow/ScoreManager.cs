/*
====================================================================
* ScoreManager - Multiplayer Score Tracking System
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
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
* - Kill reward value (10 points)
* - Wave bonus value (50 points)
* - Combined team score concept
* - Individual player score tracking
* 
* [AI-ASSISTED]
* - SyncVar score synchronization
* - Server-authority score updates
* - Event system for HUD updates
* - SyncDictionary for player scores
* 
* [AI-GENERATED]
* - NetworkBehaviour FishNet implementation
* - Complete implementation structure
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour, SyncVar, SyncDictionary)
* 
* NOTES:
* - Tracks both team score and individual player scores
* - Uses NetworkObject ID for player identification
* - Supports kamikaze deaths (team score only, no individual credit)
====================================================================
*/

using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Values")]
    [SerializeField] private int killReward = 10;
    [SerializeField] private int waveClearBonus = 50;

    [Header("Synchronized Scores")]
    private readonly SyncVar<int> teamScore = new SyncVar<int>(0);
    private readonly SyncVar<int> totalKills = new SyncVar<int>(0);
    private readonly SyncDictionary<int, int> playerScores = new SyncDictionary<int, int>();

    public delegate void ScoreChanged(int newScore);
    public event ScoreChanged OnScoreChanged;

    public delegate void PlayerScoreChanged(GameObject player, int newScore);
    public event PlayerScoreChanged OnPlayerScoreChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        teamScore.OnChange += HandleScoreChange;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        teamScore.OnChange -= HandleScoreChange;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        teamScore.Value = 0;
        totalKills.Value = 0;
    }

    [Server]
    public void AddKillScore(GameObject killerPlayer = null)
    {
        teamScore.Value += killReward;
        totalKills.Value++;

        if (killerPlayer != null)
        {
            FishNet.Object.NetworkObject netObj = killerPlayer.GetComponent<FishNet.Object.NetworkObject>();
            if (netObj != null)
            {
                int playerId = netObj.ObjectId;
                if (!playerScores.ContainsKey(playerId))
                {
                    playerScores.Add(playerId, 0);
                }

                playerScores[playerId] += killReward;
                OnPlayerScoreChanged?.Invoke(killerPlayer, playerScores[playerId]);
            }
        }
    }

    [Server]
    public void AddWaveBonus(int waveNumber)
    {
        teamScore.Value += waveClearBonus;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int points)
    {
        teamScore.Value += points;
    }

    private void HandleScoreChange(int prev, int next, bool asServer)
    {
        OnScoreChanged?.Invoke(next);
    }

    [Server]
    public void ResetScore()
    {
        teamScore.Value = 0;
        totalKills.Value = 0;
        playerScores.Clear();
    }

    public int GetTeamScore() => teamScore.Value;
    public int GetTotalKills() => totalKills.Value;
    public int GetKillReward() => killReward;
    public int GetWaveBonus() => waveClearBonus;

    public int GetPlayerScore(GameObject player)
    {
        if (player == null) return 0;
        FishNet.Object.NetworkObject netObj = player.GetComponent<FishNet.Object.NetworkObject>();
        if (netObj == null) return 0;
        int playerId = netObj.ObjectId;
        return playerScores.ContainsKey(playerId) ? playerScores[playerId] : 0;
    }
}
