using UnityEngine;

/// <summary>
/// Centralized audio settings persistence and management
/// Author: Julian Gomez | SRH Hochschule Heidelberg | January 28, 2026
/// </summary>
public static class AudioSettings
{
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const float DEFAULT_VOLUME = 0.7f;

    private static float? cachedMasterVolume;
    private static float? cachedMusicVolume;
    private static float? cachedSFXVolume;

    public static float MasterVolume
    {
        get
        {
            if (!cachedMasterVolume.HasValue)
                cachedMasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_VOLUME);
            return cachedMasterVolume.Value;
        }
        set
        {
            cachedMasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, cachedMasterVolume.Value);
            PlayerPrefs.Save();
            ApplySettings();
        }
    }

    public static float MusicVolume
    {
        get
        {
            if (!cachedMusicVolume.HasValue)
                cachedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_VOLUME);
            return cachedMusicVolume.Value;
        }
        set
        {
            cachedMusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, cachedMusicVolume.Value);
            PlayerPrefs.Save();
            ApplySettings();
        }
    }

    public static float SFXVolume
    {
        get
        {
            if (!cachedSFXVolume.HasValue)
                cachedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_VOLUME);
            return cachedSFXVolume.Value;
        }
        set
        {
            cachedSFXVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, cachedSFXVolume.Value);
            PlayerPrefs.Save();
            ApplySettings();
        }
    }

    public static float FinalMusicVolume => MasterVolume * MusicVolume;
    public static float FinalSFXVolume => MasterVolume * SFXVolume;

    public static void ApplySettings()
    {
        GameAudioManager game = GameAudioManager.Instance;
        if (game != null)
        {
            game.SetMusicVolume(FinalMusicVolume);
            game.SetSFXVolume(FinalSFXVolume);
        }
    }

    public static void ResetToDefaults()
    {
        MasterVolume = DEFAULT_VOLUME;
        MusicVolume = DEFAULT_VOLUME;
        SFXVolume = DEFAULT_VOLUME;
    }
}
