using UnityEngine;

/// <summary>
/// Centralized music manager for Menu, Lobby, and Gameplay
/// Singleton pattern with DontDestroyOnLoad persistence
/// Author: Julian Gomez | SRH Hochschule Heidelberg | January 28, 2026
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Music Tracks")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip lobbyMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("SFX Tracks")]
    [SerializeField] private AudioClip buttonClickSFX;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public enum GameState { Menu, Lobby, Playing }
    private GameState currentState = GameState.Menu;

    void Awake()
    {
        // CRITICAL: Check if instance exists BEFORE accessing
        if (Instance != null && Instance != this)
        {
            Debug.Log($"GameAudioManager: Destroying duplicate instance on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"GameAudioManager: Instance created on {gameObject.name} and marked DontDestroyOnLoad");

        // Initialize AudioSources
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    void Start()
    {
        // Only execute Start() on the persistent instance
        if (Instance != this) return;

        Debug.Log($"GameAudioManager: MenuMusic assigned: {(menuMusic != null ? menuMusic.name : "NULL")}");
        Debug.Log($"GameAudioManager: LobbyMusic assigned: {(lobbyMusic != null ? lobbyMusic.name : "NULL")}");
        Debug.Log($"GameAudioManager: GameplayMusic assigned: {(gameplayMusic != null ? gameplayMusic.name : "NULL")}");

        ApplyVolumeSettings();
        SetGameState(currentState);
    }

    public void SetGameState(GameState newState)
    {
        Debug.Log($"GameAudioManager: SetGameState {currentState} → {newState}");
        currentState = newState;

        switch (newState)
        {
            case GameState.Menu:
                PlayMusic(menuMusic);
                break;
            case GameState.Lobby:
                PlayMusic(lobbyMusic);
                break;
            case GameState.Playing:
                PlayMusic(gameplayMusic);
                break;
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("GameAudioManager: PlayMusic: clip is NULL");
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            Debug.Log($"GameAudioManager: Already playing {clip.name}, skipping");
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
        Debug.Log($"GameAudioManager: Now playing {clip.name}");
    }

    public void ApplyVolumeSettings()
    {
        float finalMusicVolume = AudioSettings.FinalMusicVolume;
        musicSource.volume = finalMusicVolume;
        Debug.Log($"GameAudioManager: Applied volume {finalMusicVolume}");
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }

    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
            sfxSource.PlayOneShot(buttonClickSFX, AudioSettings.FinalSFXVolume);
    }
}
