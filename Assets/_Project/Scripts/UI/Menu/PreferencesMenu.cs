using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Preferences Menu UI Controller
/// Author: Julian Gomez | SRH Hochschule Heidelberg | January 28, 2026
/// </summary>
public class PreferencesMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject closeButton;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Volume Labels - Optional")]
    [SerializeField] private TMP_Text masterVolumeLabel;
    [SerializeField] private TMP_Text musicVolumeLabel;
    [SerializeField] private TMP_Text sfxVolumeLabel;

    [Header("Test SFX - Optional")]
    [SerializeField] private AudioClip testSFXClip;

    private CanvasGroup canvasGroup;
    private AudioSource testAudioSource;

    void Awake()
    {
        canvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = menuPanel.AddComponent<CanvasGroup>();

        testAudioSource = gameObject.AddComponent<AudioSource>();
        testAudioSource.playOnAwake = false;
    }

    void Start()
    {
        menuPanel.SetActive(true);
        HideMenu();
        LoadSettings();

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void LoadSettings()
    {
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();

        masterVolumeSlider.value = AudioSettings.MasterVolume;
        musicVolumeSlider.value = AudioSettings.MusicVolume;
        sfxVolumeSlider.value = AudioSettings.SFXVolume;

        UpdateLabels();

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void UpdateLabels()
    {
        if (masterVolumeLabel != null)
            masterVolumeLabel.text = $"{Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
        if (musicVolumeLabel != null)
            musicVolumeLabel.text = $"{Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
        if (sfxVolumeLabel != null)
            sfxVolumeLabel.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
    }

    public void ShowMenu()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        LoadSettings();
    }

    public void CloseMenu()
    {
        HideMenu();
    }

    private void HideMenu()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioSettings.MasterVolume = value;
        UpdateLabels();
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioSettings.MusicVolume = value;
        UpdateLabels();
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioSettings.SFXVolume = value;
        UpdateLabels();
    }

    public void PlayTestSFX()
    {
        if (testSFXClip != null)
        {
            testAudioSource.volume = AudioSettings.FinalSFXVolume;
            testAudioSource.PlayOneShot(testSFXClip);
        }
        else if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayButtonClick();
        }
    }

    public void ResetToDefaults()
    {
        AudioSettings.ResetToDefaults();
        LoadSettings();
    }
}
