using UnityEngine;

/// <summary>
/// Menu Audio Controller for Menue scene
/// Author: Julian Gomez | SRH Hochschule Heidelberg | January 28, 2026
/// </summary>
public class MenuAudioController : MonoBehaviour
{
    public static MenuAudioController Instance { get; private set; }

    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        PlayMenuMusic();
        SetVolume(AudioSettings.FinalMusicVolume); // ADD THIS LINE
    }


    public void PlayMenuMusic()
    {
        if (menuMusic != null && audioSource != null)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void StopMenuMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // NEW: Set volume method (called by AudioSettings)
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
