# SHOWROOM TANGO - Game Design Document v1.6

**Project:** Showroom Tango (Bullet_Love)
**Type:** 2-Player Cooperative Bullet-Hell
**Platform:** Unity 6000.0.62f1 + FishNet 4.x Networking
**Academic Context:** PRG Course, SRH Hochschule Heidelberg
**Developer:** Julian Gomez
**Last Updated:** January 28, 2026
**Version:** v1.6

---

## 1. EXECUTIVE SUMMARY

### Core Concept
Cooperative top-down bullet-hell shooter where 2 players fight 3 waves of enemies on a shared screen. Neon retrofuturistic aesthetic with auto-fire weapon system.

### Victory Condition
Survive all 3 waves with at least one player alive.

---

## 2. CURRENT IMPLEMENTATION STATUS

### Completed Features
- FishNet multiplayer (Host + Client)
- Single-scene architecture (Menu → Game with LobbyRoot/GameRoot)
- Player movement, rotation, auto-fire weapons
- 2 enemy types (Chaser, Shooter)
- 3-wave spawning system with ObserversRpc client notification
- HP system with damage/death
- Score system with SQL/PHP backend (XAMPP)
- Highscore persistence and display
- Lobby system with player customization (name, color, ready)
- Audio system with preferences
- Story popup

### In Progress
- Wave transition UI (client sync issue)
- Audio clip assignments in Inspector

---

## 3. SCRIPT ARCHITECTURE

### Folder Structure (Cleaned January 28, 2026)
```
Assets/_Project/Scripts/
├── Enemies/
│   ├── EnemyChaser.cs         - Follow-player AI
│   ├── EnemyHealth.cs         - Server-authoritative HP
│   ├── EnemyShooter.cs        - Ranged AI with bullets
│   └── EnemySpawner.cs        - Wave system + ObserversRpc
│
├── Gameflow/
│   ├── GameStateManager.cs    - Lobby/Playing state machine
│   ├── MenuManager.cs         - Menu scene controller
│   ├── ScoreManager.cs        - Score tracking (SyncVar)
│   └── GameAudioManager.cs    - DontDestroyOnLoad audio singleton
│
├── Network/
│   ├── AutoStartNetwork.cs    - FishNet auto-host
│   ├── NetworkUIManager.cs    - Connection UI
│   └── PlayerSpawner.cs       - Player instantiation
│
├── Persistence/
│   ├── HighscoreManager.cs    - PHP/SQL communication
│   └── AudioSettings.cs       - PlayerPrefs volume persistence
│
├── Player/
│   ├── CameraFollow.cs        - Cinemachine owner tracking
│   ├── NeonGlowController.cs  - Dual-sprite glow system
│   ├── PlayerController.cs    - Movement + input
│   ├── PlayerHealth.cs        - HP system
│   ├── WeaponConfig.cs        - ScriptableObject weapon data
│   └── WeaponManager.cs       - Auto-fire multi-weapon
│
├── Projectiles/
│   ├── Bullet.cs              - Projectile behavior
│   └── BulletPool.cs          - Object pooling
│
├── UI/
│   ├── HighscoreUI.cs         - Top 10 display
│   ├── HUDManager.cs          - HP/Score/Wave display
│   ├── WaveTransitionUI.cs    - Between-wave countdown
│   ├── Lobby/
│   │   ├── LobbyManager.cs    - Player ready system
│   │   ├── LobbyUI.cs         - Lobby canvas controller
│   │   └── PlayerSetupUI.cs   - Name/color input panels
│   └── Menu/
│       ├── PreferencesMenu.cs - Volume sliders UI
│       ├── StoryPopup.cs      - Lore display
│       └── Menu-Title/
│           ├── ButtonHover.cs      - Button effects
│           └── SeamlessMenuVideo.cs - Background video
│
└── Z-Parkplatz/               - Deprecated scripts (safe to delete)
    ├── BootstrapPersist.cs
    ├── HideOnConnection.cs
    ├── SceneLoader.cs
    ├── MenuAudioController.cs  - Replaced by GameAudioManager
    └── PreferencesManager.cs   - Replaced by PreferencesMenu
```

### Audio System Architecture (v1.6)
```
GameAudioManager (DontDestroyOnLoad singleton)
├── Music Tracks: Menu, Lobby, Gameplay
├── SFX: Button clicks
├── States: Menu → Lobby → Playing
└── Volume: Via AudioSettings.cs (PlayerPrefs)

AudioSettings.cs (Static utility)
├── MasterVolume, MusicVolume, SFXVolume
├── FinalMusicVolume = Master × Music
├── PlayerPrefs persistence
└── ApplySettings() → GameAudioManager

PreferencesMenu.cs (UI Controller)
├── 3 sliders (Master, Music, SFX)
├── Real-time volume adjustment
└── CanvasGroup visibility pattern
```

---

## 4. NETWORK ARCHITECTURE

### FishNet Patterns Used
- **SyncVar:** playerName, currentHP, score, wave count
- **SyncDictionary:** Player lobby data
- **ServerRpc:** Player actions (fire, ready, name change)
- **ObserversRpc:** Wave cleared notification, game state changes

### Scene Flow
```
Menue.unity → LoadScene("Game") → Game.unity
                                   ├── LobbyRoot (active during Lobby)
                                   └── GameRoot (active during Playing)
```

---

## 5. SCENES

### Menue.unity
- Menu canvas with Start, Story, Preferences buttons
- GameAudioManager (first instance, persists)
- Video background

### Game.unity
- NetworkManager
- LobbyRoot (player setup panels)
- GameRoot (play area, enemies, HUD)
- GameStateManager controls visibility

---

## 6. KNOWN ISSUES

### Open
1. **Wave Transition UI** - Not showing on clients (event sync issue)
2. **Projectile spawn position** - Bullets spawn behind player
3. **Build UI displacement** - Menu buttons offset in build

### Resolved (v1.6)
- Highscore names showing "Player" → Fixed via initial name send
- Preferences popup not showing → Fixed CanvasGroup integration
- Audio redundancy → Consolidated to single GameAudioManager

---

## 7. GRADING REQUIREMENTS MAPPING

| Category | Points | Status |
|----------|--------|--------|
| Multiplayer Basis | 10/10 | DONE |
| Player Control | 15/15 | DONE |
| Shooting System | 20/20 | DONE |
| Enemy System | 15/15 | DONE |
| Health & Gameflow | 10/10 | DONE |
| HUD & Score | 10/10 | DONE |
| **Base Total** | **80/80** | **DONE** |
| Lobby & Matchmaking | 5-10 | DONE |
| Extended Persistence | 5-10 | DONE (SQL/PHP) |
| Visual/Technical | 5-10 | PARTIAL |
| **Estimated Total** | **~90/100** | |

---

## 8. VERSION HISTORY

### v1.6 (January 28, 2026)
- Audio system consolidated (GameAudioManager with DontDestroyOnLoad)
- Deprecated MenuAudioController, PreferencesManager → Z-Parkplatz
- Script folder structure reorganized
- Preferences popup CanvasGroup fix
- Highscore name submission fix
- Wave transition ObserversRpc implementation

### v1.5 (January 26, 2026)
- Single-scene architecture (Lobby + Game merged)
- Event-driven lobby initialization
- BulletPool refactored to MonoBehaviour

### v1.4 (January 21, 2026)
- Wave system reduced to 3 waves
- Restart button network fix
- Enemy targeting AI improvements

---

## 9. CREDITS

**Developer:** Julian Gomez
**Course:** PRG - Game & Multimedia Design
**Institution:** SRH Hochschule Heidelberg

**Technology:**
- Unity 6000.0.62f1
- FishNet 4.x
- XAMPP (PHP/MySQL)

**Assets:**
- Sprites: Kenney Space Shooter Redux (CC0)
- Font: Electronic Highway Sign

**AI Assistance:**
- Claude (Anthropic) - Technical consultation
- AI-assisted sections documented in code headers

---

*END OF DOCUMENT v1.6*
