/*
====================================================================
* SeamlessMenuVideo - Seamless Video Loop System with Dual Players
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 2.1
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Requirement for seamless menu video loop
* - Dual VideoPlayer architecture concept
* - Timing parameters (preloadOffset)
* 
* [AI-ASSISTED]
* - State machine architecture implementation
* - Event-driven VideoPlayer setup (prepareCompleted, frameReady)
* - Dynamic RenderTexture resolution matching
* - Comprehensive error handling and validation
* - Event cleanup pattern (OnDestroy)
* - Null-safety and defensive programming
* 
* [AI-GENERATED]
* - Complete implementation structure
* 
* DEPENDENCIES:
* - UnityEngine.Video (VideoPlayer, VideoClip)
* - UnityEngine.UI (RawImage)
* - UnityEngine.Events (UnityEvent)
* 
* FIXES IMPLEMENTED (v2.0):
* - Bug 1: Explicit RenderTexture creation with validation
* - Bug 2: Dynamic resolution matching video clip dimensions
* - Bug 3: Hybrid swap (event-driven + time-based fallback)
* - Added comprehensive null-safety validation
* - Added VideoPlayer error handlers
* - Added proper event cleanup in OnDestroy
* - Implemented state machine for clear lifecycle management
* 
* QA REVIEW STATUS: External expert review passed (2025-01-23)
* 
* PRODUCTION FIX (v2.1):
* - Bug: frameReady event not firing reliably
* - Solution: Hybrid approach - event-driven primary, time-based fallback
* - Swap triggers after 100ms AND frame > 2 (whichever comes first)
* - Tested with 18s main clip + 4.92s loop clip
* 
* NOTES:
* - PlayerA plays mainClip once, PlayerB loops loopClip
* - RenderTextures dynamically match video dimensions
* - Swap occurs after PlayerB renders 3+ frames (prevents black flash)
* - All events properly cleaned up to prevent memory leaks
====================================================================
*/

using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.Events;

public class SeamlessMenuVideo : MonoBehaviour
{
    [Header("Video Players")]
    [SerializeField] private VideoPlayer playerA;
    [SerializeField] private VideoPlayer playerB;
    [SerializeField] private RawImage displayImage;

    [Header("Video Clips")]
    [SerializeField] private VideoClip mainClip;
    [SerializeField] private VideoClip loopClip;

    [Header("Timing Control")]
    [SerializeField] private float preloadOffset = 0.15f;
    [SerializeField] private float clipStartOffset = 0f;

    [Header("Events")]
    public UnityEvent onVideoSystemReady;

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Debug Options")]
    [SerializeField] private bool enableDebugLogs = true;
    #endif

    private enum VideoState { Initializing, MainPlaying, Transitioning, Looping }
    private VideoState currentState = VideoState.Initializing;

    private RenderTexture rtA;
    private RenderTexture rtB;

    private bool playerAPrepared = false;
    private bool playerBPrepared = false;
    private bool hasPreloaded = false;
    private bool hasSwapped = false;

    void Start()
    {
        if (!ValidateComponents())
        {
            LogError("Component validation failed, disabling SeamlessMenuVideo");
            enabled = false;
            return;
        }

        playerA.prepareCompleted += OnPlayerAPrepared;
        playerB.prepareCompleted += OnPlayerBPrepared;
        playerA.errorReceived += OnVideoError;
        playerB.errorReceived += OnVideoError;

        playerA.clip = mainClip;
        playerA.isLooping = false;
        playerA.renderMode = VideoRenderMode.RenderTexture;
        playerA.playOnAwake = false;
        playerA.time = clipStartOffset;

        playerB.clip = loopClip;
        playerB.isLooping = true;
        playerB.renderMode = VideoRenderMode.RenderTexture;
        playerB.playOnAwake = false;

        playerA.Prepare();
        playerB.Prepare();
        currentState = VideoState.Initializing;
    }

    private bool ValidateComponents()
    {
        bool valid = true;

        if (playerA == null)
        {
            Debug.LogError("[SeamlessVideo] PlayerA not assigned in Inspector!");
            valid = false;
        }
        if (playerB == null)
        {
            Debug.LogError("[SeamlessVideo] PlayerB not assigned in Inspector!");
            valid = false;
        }
        if (displayImage == null)
        {
            Debug.LogError("[SeamlessVideo] DisplayImage (RawImage) not assigned in Inspector!");
            valid = false;
        }
        if (mainClip == null)
        {
            Debug.LogError("[SeamlessVideo] MainClip (VideoClip) not assigned in Inspector!");
            valid = false;
        }
        if (loopClip == null)
        {
            Debug.LogError("[SeamlessVideo] LoopClip (VideoClip) not assigned in Inspector!");
            valid = false;
        }

        return valid;
    }

    private void OnPlayerAPrepared(VideoPlayer source)
    {
        // Dynamic resolution matching (prevents distortion)
        rtA = CreateRenderTexture((int)source.width, (int)source.height, "PlayerA");
        if (rtA == null)
        {
            LogError("Failed to create RenderTexture A, using fallback");
            rtA = CreateRenderTexture(1920, 1080, "PlayerA_Fallback");
        }

        playerA.targetTexture = rtA;
        displayImage.texture = rtA;
        playerAPrepared = true;
        CheckBothPrepared();
    }

    private void OnPlayerBPrepared(VideoPlayer source)
    {
        // Dynamic resolution matching (handles different clip sizes)
        rtB = CreateRenderTexture((int)source.width, (int)source.height, "PlayerB");
        if (rtB == null)
        {
            LogError("Failed to create RenderTexture B, using fallback");
            rtB = CreateRenderTexture(1920, 1080, "PlayerB_Fallback");
        }

        // Event-driven swap detection (primary mechanism)
        playerB.frameReady += OnPlayerBFrameReady;
        playerB.targetTexture = rtB;
        playerBPrepared = true;
        CheckBothPrepared();
    }

    private RenderTexture CreateRenderTexture(int width, int height, string debugName)
    {
        if (width <= 0 || height <= 0)
        {
            LogError($"Invalid RenderTexture dimensions for {debugName}: {width}x{height}");
            return null;
        }

        RenderTexture rt = new RenderTexture(width, height, 0);
        rt.name = debugName;

        if (!rt.Create())
        {
            LogError($"RenderTexture.Create() failed for {debugName} ({width}x{height})");
            return null;
        }

        return rt;
    }

    private void CheckBothPrepared()
    {
        if (playerAPrepared && playerBPrepared)
        {
            playerA.Play();
            currentState = VideoState.MainPlaying;
            onVideoSystemReady?.Invoke();
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        LogError($"VideoPlayer error on {source.name}: {message}");
        enabled = false;
    }

    void Update()
    {
        // Preload loop clip before main ends
        if (currentState == VideoState.MainPlaying && !hasPreloaded && playerA.isPlaying)
        {
            double timeRemaining = playerA.length - playerA.time;
            if (timeRemaining <= preloadOffset)
            {
                playerB.Play();
                hasPreloaded = true;
                currentState = VideoState.Transitioning;
            }
        }

        // Hybrid swap: time-based fallback (100ms + 3 frames)
        if (currentState == VideoState.Transitioning && !hasSwapped)
        {
            if (playerB.isPlaying && playerB.time >= 0.1 && playerB.frame > 2)
            {
                PerformSwap($"time-based at {playerB.time:F3}s, frame {playerB.frame}");
            }
        }
    }

    private void OnPlayerBFrameReady(VideoPlayer source, long frameIdx)
    {
        // Event-driven swap (primary - waits for 3 frames to prevent black flash)
        if (!hasSwapped && frameIdx > 2)
        {
            PerformSwap($"event-based at frame {frameIdx}");
        }
    }

    private void PerformSwap(string triggerReason)
    {
        displayImage.texture = rtB;
        hasSwapped = true;
        playerA.Stop();
        currentState = VideoState.Looping;

        if (playerB != null)
        {
            playerB.frameReady -= OnPlayerBFrameReady;
        }
    }

    void OnDestroy()
    {
        // Event cleanup (prevents memory leaks)
        if (playerA != null)
        {
            playerA.prepareCompleted -= OnPlayerAPrepared;
            playerA.errorReceived -= OnVideoError;
        }

        if (playerB != null)
        {
            playerB.prepareCompleted -= OnPlayerBPrepared;
            playerB.frameReady -= OnPlayerBFrameReady;
            playerB.errorReceived -= OnVideoError;
        }

        // GPU resource cleanup
        if (rtA != null)
        {
            rtA.Release();
        }
        if (rtB != null)
        {
            rtB.Release();
        }
    }

    private void LogDebug(string message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (enableDebugLogs)
        {
            Debug.Log($"[SeamlessVideo] {message}");
        }
        #endif
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SeamlessVideo] {message}", this);
    }
}
