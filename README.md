# Showroom Tango - 2-Player Cooperative Bullet-Hell

**Developer:** Julian Gomez
**Course:** PRG - Game & Multimedia Design, SRH Hochschule Heidelberg
**Technology:** Unity 6000.0.62f1 + FishNet 4.x Networking
**Version:** v1.6
**Last Updated:** 2026-01-28

---

## Kurzbeschreibung

Showroom Tango ist ein kooperativer Top-Down Bullet-Hell-Shooter fuer 2 Spieler. Beide Spieler kaempfen gemeinsam gegen 3 Wellen von Gegnern auf einem geteilten Bildschirm. Das Spiel features automatisches Zielsystem mit bis zu 3 Waffen, Neon-Retrofuturistik-Aesthetik, und netzwerkbasiertes Multiplayer ueber FishNet.

---

## Anleitung zum Starten

### Host Starten:
1. Unity oeffnen und `Menue.unity` Scene laden
2. Play druecken
3. Auf "Start Game" klicken
4. Im Lobby als "Host" verbinden
5. Spielername eingeben und "Ready" druecken
6. Warten bis Client verbindet und auch "Ready" ist
7. Spiel startet automatisch nach 3 Sekunden Countdown

### Client Verbinden:
1. Zweite Unity-Instanz oeffnen (oder Build starten)
2. Play druecken
3. Auf "Start Game" klicken
4. Im Lobby als "Client" verbinden (localhost)
5. Spielername eingeben und "Ready" druecken
6. Spiel startet sobald beide Spieler bereit sind

**Steuerung:**
- WASD: Bewegung
- Maus: Rotation
- Waffen: Auto-Fire (automatisch auf naechste Gegner)

---

## Technischer Ueberblick

### Architektur (Single-Scene Pattern)

Das Spiel verwendet ein Single-Scene-Architektur ab Game.unity:
```
Menue.unity -> LoadScene("Game") -> Game.unity
                                    |-- LobbyRoot (aktiv waehrend Lobby)
                                    +-- GameRoot (aktiv waehrend Playing)
```

GameStateManager steuert die Sichtbarkeit der Root-Objekte basierend auf dem aktuellen Spielzustand.

### Verwendete RPCs

**WeaponManager.cs:**
- `FireWeaponServerRpc(Vector3 position, Quaternion rotation, string bulletSpriteName)` - Schuss auf Server spawnen

**GameStateManager.cs:**
- `RequestRestartServerRpc()` - Spiel-Neustart anfordern (zurueck zu Lobby)

**LobbyManager.cs:**
- `SetPlayerReadyServerRpc(NetworkConnection, bool)` - Ready-Status setzen
- `SetPlayerNameServerRpc(NetworkConnection, string)` - Spielernamen setzen
- `SetPlayerColorServerRpc(NetworkConnection, int)` - Spielerfarbe setzen

**PlayerHealth.cs:**
- `ApplyDamage(int)` - Server-Authority Schadenssystem

**EnemySpawner.cs:**
- `NotifyWaveClearedObserversRpc(int)` - Wave-Clear Benachrichtigung an alle Clients

### Verwendete SyncVars

**PlayerHealth.cs:**
- `SyncVar<int> currentHealth` - Aktuelle Lebenspunkte
- `SyncVar<bool> isDead` - Tod-Status

**EnemyHealth.cs:**
- `SyncVar<int> currentHealth` - Gegner-Gesundheit

**EnemySpawner.cs:**
- `SyncVar<int> currentWave` - Aktuelle Welle (1-3)

**GameStateManager.cs:**
- `SyncVar<GameState> currentState` - Spielzustand (Lobby, Playing, GameOver, Victory)

**ScoreManager.cs:**
- `SyncVar<int> teamScore` - Team-Punktestand

**LobbyManager.cs:**
- `SyncDictionary<NetworkConnection, PlayerLobbyData>` - Lobby-Daten aller Spieler

### Bullet-Logik

**Pooling-System:**
- BulletPool verwaltet Bullets in Queue (separate Pools fuer Player/Enemy)
- Server spawnt Bullets via ServerManager.Spawn()
- Auto-Expansion bei Pool-Erschoepfung
- Bullets werden nach 5s Lifetime oder Collision zurueck in Pool
- DespawnType.Pool verhindert Destroy (Objekte bleiben erhalten)

**Schiesssystem:**
- WeaponManager: Auto-Fire auf bis zu 3 naechste Gegner
- Prioritaets-Targeting: Waffe 1 -> naechster Gegner, Waffe 2 -> 2.-naechster, etc.
- Server-Authority: Alle Schuesse spawnen auf Server
- Fire-Rate konfigurierbar via WeaponConfig ScriptableObjects

**Synchronisation:**
- NetworkTransform synchronisiert Bullet-Position
- Server spawnt, alle Clients sehen identische Bullets
- Hit-Detection nur server-seitig (gegen Cheating)

### Gegner-Logik

**Enemy-Typen:**
- **EnemyChaser:** Verfolgt naechsten Spieler, Kamikaze-Schaden
- **EnemyShooter:** Haelt Distanz, schiesst auf Spieler

**Spawning:**
- EnemySpawner (Server-only)
- 3 Waves: 60 -> 67 -> 107 Gegner
- 70% Chaser, 30% Shooter
- Gleichmaessiges Trickle-Spawn ueber 45s pro Wave
- Spawn-Position: Kreisfoermig am Rand (33 Units Radius)

**Wave-System:**
- Victory-Condition: Wave 3 komplett + alle Gegner tot
- ObserversRpc benachrichtigt Clients bei Wave-Clear
- GameStateManager prueft Conditions in Update()

---

## Audio System (v1.6)

Das Audio-System verwendet einen persistenten Singleton:

```
GameAudioManager (DontDestroyOnLoad)
|-- Music Tracks: Menu, Lobby, Gameplay
|-- SFX: Button clicks
|-- States: Menu -> Lobby -> Playing
+-- Volume: Via AudioSettings.cs (PlayerPrefs)
```

**AudioSettings.cs:**
- Statische Utility-Klasse fuer Volume-Persistenz
- Master, Music, SFX Volumes
- PlayerPrefs Speicherung

**PreferencesMenu.cs:**
- 3 Sliders (Master, Music, SFX)
- Echtzeit Volume-Anpassung
- CanvasGroup Visibility Pattern

---

## Persistenz

### PHP/SQL Backend (Primaer - Pflichtanforderung)

**Setup:**
1. XAMPP installieren (Apache + MySQL)
2. Datenbank erstellen: `bullethell_scores`
3. SQL-Script ausfuehren: `Documentation/PHP_Backend/database_setup.sql`
4. PHP-Files kopieren nach: `C:/xampp/htdocs/bullethell/`

**Files:**
- `submit_score.php` - POST: player_name, score -> INSERT in DB
- `get_highscores.php` - GET: Return Top 10 als JSON
- `database_setup.sql` - CREATE TABLE highscores

**Unity Integration:**
- HighscoreManager: UnityWebRequest POST/GET
- Automatisches Submit bei Game Over/Victory
- Highscore-Anzeige in UI (Top 10)

**URLs:**
- Submit: `http://localhost/bullethell/submit_score.php`
- Get: `http://localhost/bullethell/get_highscores.php`

### JSON Fallback (Notfall)

Falls PHP/SQL nicht verfuegbar:
- HighscoreManager Inspector: `useJSONFallback = true`
- Speichert in: `Application.persistentDataPath/highscores.json`
- Lokale Top-10-Liste, sortiert nach Score

**Vollstaendige Setup-Anleitung:** `Documentation/PHP_Backend/README_BACKEND_SETUP.txt`

---

## Bonusfeatures

### Implementiert:
- **Lobby System:** Spieler-Setup mit Namen, Farbe, Ready-Status (SyncDictionary)
- **Story Popup:** Lore/Hintergrundgeschichte im Menue
- **Preferences Menu:** Volume-Einstellungen mit Persistenz
- **Neon-Glow Visual System:** Dual-Sprite-Technik mit Outline-Glow
- **Multi-Weapon Auto-Fire:** Brotato-inspiriertes Waffensystem mit 3 Slots
- **Menu Polish:** Hover-Effekte, Audio-Controller, Video-Background
- **PHP/SQL Highscore:** Persistente Online-Highscores

### Nicht Implementiert:
- Power-Ups (Shield, Fire-Rate Boost)
- Komplexe Bullet-Patterns (Spiral, Ring, Homing)
- Schwierigkeitsauswahl

---

## Bekannte Bugs

### Offen (TIER 1):
- **Wave Transition UI:** Countdown zwischen Wellen erscheint nicht auf Clients
- **Projectile Spawn Position:** Bullets spawnen hinter Spieler statt vorne

### Offen (TIER 2):
- **Build UI Displacement:** Menu-Buttons verschoben in Build-Version
- **Umlaute Bug:** ae, oe, ue werden nicht korrekt dargestellt

### Geloest (v1.6):
- Highscore Namen zeigen "Player" -> Behoben via initialer Name-Send
- Preferences Popup erscheint nicht -> CanvasGroup Integration behoben
- Audio Redundanz -> Konsolidiert zu einzelnem GameAudioManager

---

## Projekt-Struktur

```
Assets/_Project/
|-- Scripts/
|   |-- Enemies/           # EnemyChaser, EnemyShooter, EnemyHealth, EnemySpawner
|   |-- Gameflow/          # GameStateManager, ScoreManager, MenuManager, GameAudioManager
|   |-- Network/           # AutoStartNetwork, NetworkUIManager, PlayerSpawner
|   |-- Persistence/       # HighscoreManager, AudioSettings
|   |-- Player/            # PlayerController, PlayerHealth, WeaponManager, CameraFollow
|   |-- Projectiles/       # Bullet, BulletPool
|   |-- UI/
|   |   |-- Root/          # HUDManager, HighscoreUI, WaveTransitionUI
|   |   |-- Lobby/         # LobbyManager, LobbyUI, PlayerSetupUI
|   |   +-- Menu/          # PreferencesMenu, StoryPopup, ButtonHover, SeamlessMenuVideo
|   +-- Z-Parkplatz/       # Deprecated scripts (safe to delete)
|-- Prefabs/
|-- Scenes/
|   |-- Menue.unity        # Hauptmenue
|   +-- Game.unity         # Lobby + Spielbereich

Documentation/
|-- PHP_Backend/           # submit_score.php, get_highscores.php, database_setup.sql
|-- GameDesignDocument.md  # Aktuelle Architektur (v1.6)
|-- OpenTasks_Prioritized.md # Bug-Tracking und Tasks
+-- _Archive/              # Alte Session-Logs
```

---

## Architektur-Entscheidungen (ADRs)

- **ADR-001:** Wave-System statt Boss (Einfacher, weniger Risiko)
- **ADR-002:** Object-Pooling ab Tag 1 (Performance)
- **ADR-003:** Single-Scene Architecture (Lobby + Game in einer Scene)
- **ADR-007:** Unity New Input System (Zukunftssicher)
- **ADR-009:** Server-Authority fuer alle Gameplay-Logik (Anti-Cheat)

---

## Entwicklungs-Attribution

**Human-Authored:**
- Spielkonzept, Mechanik-Design, Werte-Balancing
- Unity Szenen-Setup, Prefab-Erstellung
- Visual Design (Neon-Aesthetic)
- Anforderungsdefinition

**AI-Assisted:**
- Networking-Code (FishNet 4.x Integration)
- Architektur-Entscheidungen (ADRs)
- Dokumentationsstruktur
- PHP/SQL Backend-Integration
- Code-Kommentare und akademische Header
- Bug-Analyse und Debugging

**Details:** Jedes Script enthaelt detailliertes Authorship-Tracking im Header

---

## Lizenz & Attribution

Dieses Projekt wurde im Rahmen des PRG-Moduls an der SRH Hochschule Heidelberg entwickelt.

**Third-Party Assets:**
- FishNet Networking (MIT License)
- Unity Input System (Unity Companion License)
- TextMeshPro (Unity Package)
- Kenney Space Shooter Redux (CC0)
- Electronic Highway Sign Font

---

## Kontakt

**Entwickler:** Julian Gomez
**Repository:** https://github.com/JuliGommz/Bullet_Love
**Hochschule:** SRH Hochschule Heidelberg
**Kurs:** PRG - Game & Multimedia Design
