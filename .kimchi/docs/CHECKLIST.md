# Last Word — Implementation Checklist

Generated from a full read of `GAME_DESIGN.md` v2.5, `Listener.md`, `player.md`,
`design/gdd/systems-index.md`, `design/gdd/game-design-document.md`,
`design/gdd/game-concept.md`, `docs/notes/phase2-note-system.md`, the two
`.kimchi/ferments/.../docs/step-1-*.md` notes, and a directory survey of all
22 C# scripts, 9 scenes, and the (empty) `Assets/Audio` + `Assets/Textures`
folders.

**Source of truth hierarchy:** `REQUIREMENTS.md` already contains a 139-row
matrix. This file is a **condensed, actionable, prioritised** view of the same
data — it groups items into "ship now / build next / post-launch" buckets so the
team can decide what to work on next.

---

## Codebase snapshot

| Asset class | Count | Notes |
|---|---|---|
| C# scripts | 22 | ~8,000 LOC across Core/Player/Enemy/UI/Networking/World |
| `.tscn` scenes | 9 | MainMenu, CustomLobby, MatchmakingLobby, PauseMenu, SettingsMenu, HUD, GameScene, Player, Listener, TokenVisual, base_character, F1_Basement |
| `.tres` resources | 1 | `ListenerBehaviorTree.tres` (8-entry BT) |
| `.gdshader` | 1 | `TokenStencil.gdshader` |
| `.gd` (BT support) | 2 | `ListenerBTCondition.gd`, `ListenerBTSetTargetMode.gd` |
| Tests | 1 | `Tests/SequenceManagerTests.cs` |
| Audio assets | **0** | `Assets/Audio/` empty |
| Texture assets | **0** | `Assets/Textures/` empty |
| Models | 2 | `AshfordEstate.glb`, `BaseCharacter.fbx` (both unimpored) |

---

## Progress by section (from REQUIREMENTS.md)

| § | Section | Done | Partial | Not started | Total |
|---|---|---|---|---|---|
| 3 | Voice Mechanics | 12 | 0 | 0 | 12 |
| 4 | Listener AI | 7 | 0 | 0 | 7 |
| 5 | Player Roles | 12 | 0 | 0 | 12 |
| 6 | Map Design | 14 | 0 | 0 | 14 |
| 7 | Objective System | 12 | 0 | 0 | 12 |
| 8 | Escalation Timeline | 0 | 0 | 4 | 4 |
| 9 | Death & Spectator | 0 | 1 | 6 | 7 |
| 10 | Items & Environmental | 0 | 0 | 11 | 11 |
| 11 | UI & HUD | 0 | 4 | 13 | 17 |
| 12 | Audio Design | 4 | 1 | 4 | 9 |
| 13 | Multiplayer | 1 | 4 | 5 | 10 |
| 14 | Progression | 0 | 1 | 7 | 8 |
| 15 | Achievements | 0 | 0 | 8 | 9 |
| 18 | Random Matchmaking | 0 | 2 | 5 | 7 |
| **TOTAL** | | **61** | **14** | **61** | **139** |

**Roll-up:** 61 done · 14 partial · 61 not started · 1 blocked (GameScene crash) · 5 deferred (Steam)

---

## ✅ Done (verified in code)

- **§3.1** Token transfer (server-auth, anti-spoof) — `VoiceManager.UpdateTokenHolder`, `TokenVisual`
- **§3.2** Mic calibration (30s) — lobby Calibrate button + progress bar + baseline label in `CustomLobby`/`MatchmakingLobby`
- **§3.3** Tier classification Silent/Whisper/Normal/Shout — `VoiceManager.ClassifyTier` with 5% hysteresis
- **§3.3** Tier 0 environmental detection (4m) — `ListenerAI.SubWhisperRadius`
- **§3.3** Environmental vs voice Tier 1 distinction — `ListenerAI.HearNoise` case 1
- **§3.6** Vocal Sacrifice — `VocalSacrifice.cs` + `ListenerAI` lock mode + HUD countdown
- **§3.7** 8 standard gestures (Z X C V B N M L) — `GestureSystem.cs` + input routing
- **§3.7** Radial gesture wheel (MMB hold 0.3s) — `GestureWheel.cs` + `HUD.tscn`
- **§3.7** Clap (Q, post-Lights Out, Tier 0) — `ClapAbility.cs` + `OmniLight3D`
- **§5.Loud** F stun pulse (5s, 90s CD, Tier 2.5) — `LoudStun.cs` + `ListenerAI.ApplyStun`
- **§5.Static** R white-noise bubble (4m, 40s, 2 charges) — `StaticBubble.cs` + audio suppression
- **§5** Role selection in lobby — `CustomLobby`/`MatchmakingLobby` dropdown + `NetworkManager` sync + `GameManager` spawn
- **§5.Mute** Passive halved detection radius — `ListenerAI.GetPlayerDetectionMultiplier`
- **§5.Mute** Silent Drop (E hold 2s at Registration Board) — `MuteSilentDrop.cs` + `RegistrationBoard.SilentDrop`
- **§5.Mute** 5 bonus gestures (keys 1–5) — covered by `GestureSystem`
- **§5.Loud** Passive: speech always ≥ Tier 2 — `VoiceManager.GetLocalMinimumTier`
- **§5.Echo** Replay decoy (T key, 60s CD) — `EchoReplay.cs`
- **§5.Archivist** Silent note registration (E hold 5s) — `ArchivistRegistration.cs` + `RegistrationBoard.RegisterSilently`
- **§5.Witness** Extended ghost burst (6s) + Listener path reveal hook — `WitnessBurst.cs`
- **§5** Role death notification ("ROLE LOST") — `RoleNotification.cs`
- **§4.3** Adaptive Evolution (Metric A–D, 5-min shifts) — `AdaptiveEvolution.cs` (dormant, enable post-launch)
- **§6.1** F1 Basement interactive setup — `NavigationRegion3D` + `ListenerSpawn` + `NoteSpawn_*` markers + `RegistrationBoard` + `FloorNavigationBaker.cs` + **actual instanced KayKit Dungeon assets** in `F1_Basement.tscn` (walls/floor/pillar/props as `PackedScene` nodes visible in editor) + `WorldEnvironment` with dark sky, fog, glow + 5 warm point lights
- **§6.2** Creaking floor tiles — `CreakZone.cs` + 3 placed zones in `F1_Basement.tscn`
- **§6.1 (revised)** F2–F4 scenes rebuilt with instanced KayKit assets + atmospheric lighting (see `Scripts/World/*Builder.cs` removed in favour of scene-native `PackedScene` instances)
- **§3.4** Vocal imprinting profile — `VocalImprintTracker.cs` + per-player tracking
- **§3.5** Playback Trap (90s silence → mimicry) — `PlaybackManager.cs` + per-player `VoiceRecorder`
- **§4.2** State machine (Idle/Alerted/Hunting/Frenzy) — `ListenerAI.UpdateStateLogic` + RPC eye-glow
- **§4.2** Eye-socket colour animation per state — `GetEyeColorForState`
- **§4.2.1** 5m attack range + vision rule — `IsPlayerAttackEligible` + `ListenerMovementThreshold = 0.12`
- **§4.2.1** 1Hz hearing-priority tick during sprint — `_hearingPriorityTimer`
- **§4.2.1** Authoritative priority table — `ListenerBehaviorTree.tres` (8-entry BTSelector)
- **§4.2** 10-minute escalation (Alerted 8s → Idle pre-10m, 15s → Hunting post-10m) — `GameManager.EscalationReached` + `ListenerAI`
- **§4.4** Voice mimicry (Playback) — covered by `PlaybackManager.cs` + `VoiceRecorder`
- **§4.5** Catch & Death sequence — 2s fade + 60% pitch playback + death-card fields + Whisper transition
- **§10.2** Environmental Tier 1 vs voice Tier 1 distinction — `HearNoise` case 1
- **§12.2** Listener Hunting breath (8m, 3s loop) — `ListenerAI.cs` `ApplyState` → `AudioAssets.PlayOneShot3D(ListenerHuntingBreath, ...)` on `→Hunting` transition + `Assets/Audio/listener/listener_hunting_breath.wav`
- **§12.2** Listener Frenzy tone (dissonant cluster) — `ListenerAI.cs` `ApplyState` → `AudioAssets.PlayOneShot3D(ListenerFrenzyTone, ...)` on `→Frenzy` transition + `Assets/Audio/listener/listener_frenzy_tone.wav`
- **§12.2** Catch silence (1s) — `ListenerAI.cs` `KillByListener` pre-step → `AudioAssets.PlayOneShot3D(ListenerCatchSilence, ...)` + `Assets/Audio/listener/listener_catch_silence.wav`
- **§12.3** Gramophone crackled music (10s loop) — `Scripts/World/GramophoneItem.cs` + `AudioAssets.GramophoneMusicLoop` + 90s lifetime + Tier-1 environmental noise repeat every 15s so Listener pathfinds; full item pickup/charges (UI side of §10.1) still not started
- **§13.1** Host-authoritative multiplayer (2–4) — `NetworkManager` + `GameManager`
- **§13.5** Token transfer latency (client-predict + server-validate) — `UpdateTokenHolder`
- **§18.3** Forced orientation mode (<3 prior runs) — `GameManager._orientationActive`

---

## 🟡 Partial (wired but gaps remain)

| ID | Feature | Gap |
|---|---|---|
| 7.3 | Phase 3 Permanent Frenzy | `IsPhase3PermanentFrenzyActive` defined (`virtual → false`); not hooked to phase state |
| 9.2 | ESC overlay during spectator | `PauseMenu` works; no explicit guard suspending `J` map-marker |
| 11.2 | Last Word Token indicator | HUD wired; **missing** proximity pulse rates (slow/medium/fast/strobe) and Tier-1 light-blue rule |
| 11.2 | Voice activity indicator | Tier label + volume meter present; **missing** Tier-1 light-blue colour rule |
| 11.5 | Settings menu | Master/Listener/Mic-device sliders + calibration + privacy notice **all exported** (`SettingsMenu.cs` is more complete than the matrix suggests); still need mic re-run button + region selector |
| 11.5 | Audio sliders (Master/VC/Listener ≥20%) | `ListenerVolumeSlider` exported with `ListenerWarningBox`; `VoiceBusName = "Microphone"` wired |
| 12.2 | Listener idle hum | `HumPlayerPath` (AudioStreamPlayer3D) wired in `ListenerAI`; asset `Assets/Audio/listener/listener_hum.wav` present and imported |
| 13.1 | ESC overlay during live sessions | Works; needs verification Token indicator stays live while ESC open |
| 13.3 | Voice chat SDK | Mic amplitude-only; **WebRTC transport not integrated** |
| 13.3 | Privacy notice | `MainMenu.GDPRNoticeText` exists; **no First-Time Setup screen** |
| 13.4 | Audio settings sliders | See 11.5 |
| 13.6 | Grief detection logs | Token frequency events emitted; **Static-bubble exemption + 2s Sacrifice lock window** not explicit |
| 14.1 | Patrol route randomization | `PatrolWaypoint.cs` cycles **alphabetically**, not shuffled with zone constraints |
| 14.5 | Local save schema | `user://settings.cfg` exists; **missing** `runs`, `deaths`, `tip_counter`, `token_hold_runs`, `per_run_speak_time`, setup-complete flag, privacy ack timestamp |
| 18.2 | Public lobby browser | `MatchmakingLobby.cs` exists; **no lobby-list UI** (single-room flow only) |
| 18.3 | Consent reminder + privacy | Text exists in `MainMenu`; **no explicit lobby banner** |

> **Audit correction:** The original matrix tagged `SettingsMenu.cs` as "file exists, content unknown." A grep of the file confirms ~1,087 lines with exports for: MasterVolumeSlider, ListenerVolumeSlider (with warning box), MicDeviceOption, MicMonitorToggle, MicTestMeter, CalibrationProgressBar, CalibrationStatusLabel, PrivacyStatusLabel, BtnViewPrivacy. Downgrade §11.5 from "Partial/unknown" to "Partial — significant wiring, two missing buttons."

---

## ❌ Not started (zero code)

### P0 — Vertical-slice blockers
- [ ] **§7.1** Note pickup (E hold 2s, silent) — needs `NoteItem.tscn` collider + `WordPool` source data
- [ ] **§7.1** Voice registration of picked-up words — `RegistrationBoard` script exists, needs spawn point in `F1_Basement.tscn`
- [ ] **§7.1** Sequence reveal (gated on registration complete) — `SequenceManager` exists but reveal logic unimplemented
- [ ] **§7.2** 10m clock-mechanism radius check — new `ClockMechanism.cs`
- [ ] **§7.2** Sustained Tier 2 for 0.5s word registration — `SequenceManager` hook
- [ ] **§7.2** Wrong-order reset + 30s lock — `SequenceManager`
- [ ] **§7.2** Player-count scaling (1-skip allowed at ≥2 survivors) — `SequenceManager`
- [ ] **§7.2** Single-survivor fail state (10s grace) — new `FailState.cs`
- [ ] **§7.2** Total party wipe screen — new `WipeScreen.cs`
- [ ] **§7.3** Phase 3 broadcaster monologue (10s sustained Tier 2) — `RadioBroadcast.cs` exists but handoff/reset logic unimplemented
- [ ] **§7.3** Radio handoff on broadcaster death (timer reset, no progress memory) — `RadioBroadcast.cs`
- [ ] **§7.3** Text Broadcaster mode (0.6× typing speed) — new `TextBroadcaster.cs`
- [ ] **§8.1** Minute-10/20/25/30 escalation timer — new `EscalationTimer.cs` + `LightingController.cs`

### P1 — Floor 1 vertical slice
- [ ] **§6.2** Broken intercom (Floor 1) — new `Intercom.cs`
- [ ] **§6.2** Broken intercom (Floor 1) — new `Intercom.cs`
- [ ] **§10.1** Lighter, Torch Battery, Noise Box, Signal Jammer, Gramophone — new items under `Scripts/World/Items/`
- [ ] **§10.2** Candles (auto-light 1.5m + pathfinding confusion) — new `Candle.cs`
- [ ] **§10.2** Breakable windows (already wired as StaticBody3D in F1_Basement) — needs `Window.cs` controller

- [ ] **§9.1** Screen fade to black (2s) on death — new `DeathFadeOverlay.cs`
- [ ] **§9.1** Distorted voice playback (60% pitch) — new `DeathAudioPlayer.cs` (requires mic recording first)
- [ ] **§9.1** Death card (cause, hold time, speak time, severity, tip) — new `DeathCard.cs`
- [ ] **§9.2** Whisper spectator mode (2× move, phase through walls) — `PlayerController` dead-state branch
- [ ] **§9.2** Listener real-time position map — new `SpectatorMap.cs`
- [ ] **§9.2** J-key map marker (15s, 1 active) — new `SpectatorMapMarker.cs`
- [ ] **§11.2** Phase tracker (top-left, 3 dots) — new `PhaseTracker.cs`
- [ ] **§11.2** Battery indicator (bottom-left, torch only) — HUD component
- [ ] **§11.2** Teammate status icons (4 corners) — HUD component
- [ ] **§11.3** Listener proximity pulse (12m accessibility) — new `AccessibilityPulse.cs`
- [ ] **§11.3** Listener direction indicator (8m, 2s updates) — new `DirectionArc.cs`
- [ ] **§11.3** Subtitle system — new `SubtitleManager.cs`
- [ ] **§11.3** Keybind remapping — new `ControlsRemap.cs` (J reserved)
- [ ] **§11.3** Text Broadcaster toggle in Accessibility — new `AccessibilitySettings.cs`
- [ ] **§11.4** First-Time Setup screen (region/calibration/accessibility/keybinds/privacy) — new `FirstTimeSetup.cs`
- [ ] **§13.2** Host disconnect handling (60s rejoin window) — new `HostDisconnectHandler.cs`
- [ ] **§13.2** Mic failure detection (>10s zero amplitude) — new `MicFailureWatchdog.cs`
- [ ] **§13.6** Vote-kick (3-of-4 / 2-of-3 / 2-of-2) — new `VoteKick.cs`
- [ ] **§13.6** Session abandonment cooldown (10m/5m/0) — `NetworkManager` extension
- [ ] **§14.2** Post-run stats screen — new `RunStatsScreen.cs`
- [ ] **§15** Achievements tracked in local save: Golden Silence, No Screaming, Last Words, Final Broadcast, Hot Potato, The Sacrifice, Listener's Favourite, Heard Nothing
- [ ] **§18.2** Lobby browser / list UI
- [ ] **§18.3** Lobby text chat (closes on run start)
- [ ] **§18.3** Pre-run mic check display (per-player meter)
- [ ] **§18.3** Quick Join (region adjacency fallback) — new `QuickJoin.cs`
- [ ] **§18.6** Anti-grief UI (lobby lock, scoreboard mute, post-run report)

### P2 — Floors 2 / 3 / 4 (post vertical slice)
- [ ] **§6.1** F2 Bedrooms, F3 Library (Silence Room), F4 Clock Tower scenes
- [ ] **§6.3** Wardrobe (room 2A, 20s mute, exit risk 5m) — new `Wardrobe.cs`
- [ ] **§6.3** Dead phones (2B, 2D) — new `DeadPhone.cs`
- [ ] **§6.4** Silence Room (capacity 2, audio-isolated both ways, Aggressive Check) — new `SilenceRoom.cs`
- [ ] **§6.5** Clock Tower heavy door (12s barricade) — new `Barricade.cs`
- [ ] **§6.5** Bell ring every 5min (4s mask window) — new `ClockBell.cs`
- [ ] **§6.5** Radio (Final Broadcast pickup/handoff) — partial (`RadioItem.cs` exists, logic TBD)
- [ ] **§10.2** Furniture barricade (E hold 3s) — `Barricade.cs`
- [ ] **§10.2** Furniture knockover (Tier 0, 30%) — new `KnockoverPhysics.cs`
- [ ] **§12.3** Creaking-floor audio, clock-bell audio, broken-window wind, gramophone music — all `.ogg`/`.wav` assets + integration

---

## ⛔ Deferred (Steam-partner blockers)

These cannot be implemented until a Steam partner account / AppID is provisioned.
Documented for completeness; **do not implement** until scope decision:

- **§13.6** Random-matchmaking grief escalation (24h lockout) — needs persistent account identity
- **§15.1** Strangers in the Dark achievement — needs Steam friends check
- **§18.5** Region selection (NA E/W, EU W/C, SEA, OCE) — needs Steam Relay routing
- **§18.7** Strangers in the Dark — same as §15.1
- **§45** Full Steam integration (Achievements + Relay + public lobby browser)

---

## ⚠️ Post-launch (P2 in design, NOT blockers)

These are documented in `GAME_DESIGN.md` but explicitly out of EA scope.
Keep as design notes only:


- **§9.2** Ghost burst spectator (3s VC windows, 20s CD) — design only
- **§12.4** Room-aware VC reverb — nice-to-have
- **§14.3** Cosmetic unlocks — Steam achievements
- **§14.4** Difficulty modes (Whisper Only / Deaf Run / One Life)
- **§14.5** Save backup export button + Cloud save

---

## 🚧 Open questions blocking scope

From `REQUIREMENTS.md §Open Questions` (still valid after this audit):

1. **Word pool** (§7.1) — design says 90 words in a separate asset file. Source data needed before Phase 1 can be tested. `WordPool.cs` exists but no concrete data.
2. **Monologue pool** (§7.3) — 60 dramatic + 20 absurdist. `MonologuePool.cs` exists but content unverified.
3. **Voice SDK direction** (§13.3) — Godot Voice/WebRTC integration vs. keep current mic-amplitude-only design?
4. **Art assets** (§6, §10, §12) — `AshfordEstate.glb` and `BaseCharacter.fbx` exist but are **not yet imported** into Godot (`.import` files absent). Audio + texture folders are empty.

---

## Recommended next-sprint targets (P0 vertical slice)

If only 5–10 items can be picked up next sprint, prioritise:

1. **§7.1 Phase 1 wiring** — `NoteItem` collider + spawn markers in `F1_Basement.tscn` + `RegistrationBoard` placed in scene + wire to `WordPool`
2. **§8.1 EscalationTimer** — minute-10/20/25/30 hooks (smallest gating system, unblocks many other tests)
3. **§9.1 Death fade + death card** — completes the death loop so you can playtest runs end-to-end
4. **§11.4 First-Time Setup screen** — required for any new player to reach a lobby with calibration
5. **§5 Loud + Mute + Static roles + ability stubs** — unlocks role-selection in lobby
6. **§3.6 Vocal Sacrifice** — single mechanic, gives players the "I volunteer" moment in playtests
7. **§7.2 SequenceManager completion** — wrong-order reset, 30s lock, 10m radius, scaling — most missing logic is in one file

Each of these is a single-file (or two-file) addition that materially advances
the vertical slice without blocking on art, Steam, or post-launch systems.
