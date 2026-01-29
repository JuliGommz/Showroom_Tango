/*
====================================================================
* AudioSettings - Audio Settings Persistence System
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2026-01-28
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Three-tier volume system (Master, Music, SFX)
* - Default volume (0.7)
* - PlayerPrefs persistence
* 
* [AI-ASSISTED]
* - Static class pattern
* - Caching strategy for performance
* - Final volume calculation (Master * Category)
* 
* [AI-GENERATED]
* - Property implementation with caching
* - GameAudioManager integration
* 
* DEPENDENCIES:
* - GameAudioManager (volume application)
* 
* NOTES:
* - Static class for global access
* - Lazy-loaded caching prevents redundant PlayerPrefs reads
* - Master volume multiplied with category volumes for final output
====================================================================
*/

using UnityEngine;

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
