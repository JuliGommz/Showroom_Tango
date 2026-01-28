# Showroom Tango - Open Tasks & Priority List
**Last Updated:** 2026-01-28
**Status:** Active Development
**Version:** v1.6

---

## TIER 1 - URGENT (Grade-critical)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1 | SQL/PHP Highscore Backend | DONE | Deployed XAMPP + PHP, DB created, URLs fixed, score submission integrated |
| 2 | Projectile spawn position bug | OPEN | Bullets spawn from behind player instead of front |
| 3 | Music Manager | DONE | GameAudioManager with DontDestroyOnLoad, 3 states (Menu/Lobby/Playing) |
| 4 | README.md update | OPEN | Must reflect final state: RPCs, SyncVars, persistence |
| 5 | Wave Transition UI not showing | OPEN | See BACKLOG - ObserversRpc implemented but still not working |

## TIER 2 - HIGH (Secures bonus points)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 6 | Build UI displacement bug | OPEN | Menu buttons displaced in Build version |
| 7 | Umlaute bug (ae, oe, ue) | OPEN | Text input encoding issue |
| 8 | Story Popup | DONE | StoryPopup.cs with CanvasGroup visibility |
| 9 | Preferences Menu | DONE | PreferencesMenu.cs with volume sliders |
| 10 | Player Colors | OPEN | Sprite-based visual distinction (manual Unity setup) |

## TIER 3 - MEDIUM (Improves grade)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 11 | Difficulty System (3 levels) | OPEN | L1: baseline, L2: +10% spawn, L3: additional +10% |
| 12 | Score balancing per difficulty | OPEN | Reward scaling |
| 13 | Difficulty Choice UI | OPEN | Selection method TBD |

## TIER 4 - LOW (Polish)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 14 | Graphics settings in Preferences | OPEN | If feasible |
| 15 | Clean up repo | OPEN | Remove Build_Testing/, untracked files |
| 16 | Delete Z-Parkplatz deprecated scripts | OPEN | Safe to remove after commit |

---

## Completed Items (Session January 28, 2026)

| Task | Status | Notes |
|------|--------|-------|
| Audio System Consolidation | DONE | Merged to single GameAudioManager with DontDestroyOnLoad |
| Preferences Popup Fix | DONE | MenuManager now calls PreferencesMenu.ShowMenu() |
| Highscore Name Fix | DONE | PlayerSetupUI sends initial name on registration |
| Script Structure Cleanup | DONE | Moved deprecated scripts to Z-Parkplatz |
| GDD Update | DONE | v1.6 with current architecture |

---

## BACKLOG - Detailed Bug Reports

### #5 - Wave Transition UI Not Showing Between Waves

**Status:** OPEN
**Priority:** TIER 1
**Last Attempted:** 2026-01-28

**Problem:**
`WaveTransitionUI.cs` should display countdown between waves but nothing appears.

**Root Cause:**
C# events don't cross FishNet network boundary. Server fires event, clients don't receive.

**Current Implementation (v1.2):**
- `EnemySpawner.OnWaveCleared` event
- `NotifyWaveClearedObserversRpc()` sends to clients
- Still not working after ObserversRpc implementation

**Investigation Needed:**
1. Check Inspector references (countdownPanel, countdownText)
2. Verify FindAnyObjectByType finds networked EnemySpawner
3. Check console for RPC receipt log
4. Test Host-only (single player) first

**Files:**
- `EnemySpawner.cs` (v1.2)
- `WaveTransitionUI.cs` (v1.1)

---

## Script Architecture (Current)

```
Assets/_Project/Scripts/
├── Enemies/           (4 scripts)
├── Gameflow/          (4 scripts - includes GameAudioManager)
├── Network/           (3 scripts)
├── Persistence/       (2 scripts - includes AudioSettings)
├── Player/            (6 scripts)
├── Projectiles/       (2 scripts)
├── UI/
│   ├── Root/          (3 scripts)
│   ├── Lobby/         (3 scripts)
│   └── Menu/          (2 scripts + Menu-Title subfolder)
└── Z-Parkplatz/       (5 deprecated scripts)
```

---

## Anforderungen Coverage Summary

| Pflicht Category (80 pts) | Points | Status |
|---|---|---|
| 1. Multiplayer-Basis (FishNet) | 10 | DONE |
| 2. Spielersteuerung & Sync | 15 | DONE |
| 3. Bullet-Hell Shooting | 20 | DONE |
| 4. Gegner/Wave System | 15 | DONE |
| 5. Leben & Gameflow | 10 | DONE |
| 6. HUD & Score | 10 | DONE |
| **Base Total** | **80/80** | **DONE** |

| Bonus Category (max 20 pts) | Status |
|---|---|
| A) Lobby & Matchmaking | DONE (5-10 pts) |
| B) Power-Ups | NOT IMPLEMENTING |
| C) Complex Patterns | NOT IMPLEMENTING |
| D) Extended Persistence | DONE (SQL/PHP) |
| E) Coop/Versus | NOT IMPLEMENTING |
| F) Visual/Technical | DONE (Audio system) |

**Estimated current score: ~85-90 / 100**

---

## Unity Inspector Actions Required

After this session, manually verify in Unity:
1. **MenuManager** - Assign PreferencesMenu reference (changed from GameObject)
2. **GameAudioManager** - Assign Menu, Lobby, Gameplay AudioClips
3. **WaveTransitionUI** - Verify countdownPanel and countdownText assigned

---

## Next Session Priority

1. Debug Wave Transition UI (add Debug.Log in HandleWaveCleared)
2. Fix projectile spawn position bug
3. Update README.md
4. Final testing and cleanup

---

*Document maintained for session continuity*
