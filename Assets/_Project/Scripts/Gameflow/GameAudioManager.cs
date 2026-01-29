/*
====================================================================
* GameAudioManager - Audio Management System
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2026-01-29
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Two-track music system (Menu/Gameplay)
* - Scene-based automatic switching
* - Volume control integration
* 
* [AI-ASSISTED]
* - Singleton pattern implementation
* - Scene change detection
* - AudioSettings integration
* 
* [AI-GENERATED]
* - Complete implementation structure
* 
* DEPENDENCIES:
* - UnityEngine.SceneManagement
* - AudioSettings (static volume settings)
* 
* NOTES:
* - Persists across scenes via DontDestroyOnLoad
* - Automatically switches music based on scene name
* - Supports "Menue" or "Menu" scene naming
====================================================================
*/

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Music Tracks")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("SFX")]
    [SerializeField] private AudioClip buttonClickSFX;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    void Start()
    {
        if (Instance != this) return;

        ApplyVolumeSettings();
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayMusicForCurrentScene();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForCurrentScene();
    }

    private void PlayMusicForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Menue" || sceneName == "Menu")
        {
            PlayMusic(menuMusic);
        }
        else
        {
            PlayMusic(gameplayMusic);
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void ApplyVolumeSettings()
    {
        musicSource.volume = AudioSettings.FinalMusicVolume;
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
