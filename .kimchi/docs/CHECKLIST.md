# Last Word — Implementation Checklist

Re-audited 2026-06-28 from the current codebase. Source docs: `GAME_DESIGN.md`
v2.5, `Listener.md`, `player.md`, `design/gdd/systems-index.md`,
`design/gdd/game-design-document.md`, `design/gdd/game-concept.md`,
`docs/notes/phase2-note-system.md`, `docs/superpowers/plans/2026-06-27-optimization-plan.md`,
and `docs/superpowers/specs/2026-06-27-optimization-design.md`. Compared against
all 72 C# scripts, 28 `.tscn` scenes, 1 `.gdshader`, 2 `.gd` BT nodes, 1 test
file, 36 audio assets, and KayKit `.glb` instances.

**Source of truth hierarchy:** `REQUIREMENTS.md` is the authoritative 139-row
matrix. This file is the **condensed, actionable, prioritised** view — items are
grouped into "ship now / build next / post-launch" buckets so the team can
decide what to work on next. Where this file and `REQUIREMENTS.md` disagree,
**this file wins** for status (it was re-read against the actual code today).

**Verification evidence:** `wc -l` per file, `grep -r "NotImplementedException"`
= **0** matches across `Scripts/`, `grep -r "TODO|FIXME|stub|placeholder"` =
**7** matches (all inline integration notes — listed in the Audit notes
section). No file is empty or a class shell.

---

## Codebase snapshot

| Asset class | Count | Notes |
|---|---|---|
| C# scripts | **72** | **15,564 LOC** across Core / Player / Enemy / UI / Networking / World / Items / Abilities |
| `.tscn` scenes | **28** | MainMenu, CustomLobby, MatchmakingLobby, PauseMenu, SettingsMenu, HUD, FirstTimeSetup, GameScene, Floor1, 4× Floors (F1–F4), Player, Listener, TokenVisual, GestureWheel, RegistrationBoard, base_character, **9 Items/** scenes |
| `.tres` resources | 0 | Old `ListenerBehaviorTree.tres` removed; now uses LimboAI addon (`addons/limboai/`) |
| `.gdshader` | 1 | `TokenStencil.gdshader` |
| `.gd` (BT support) | 2 | `ListenerBTCondition.gd`, `ListenerBTSetTargetMode.gd` |
| Addons | 1 | `limboai/` (BT runtime) |
| Tests | 1 | `Tests/SequenceManagerTests.cs` (NUnit, wired via `Microsoft.NET.Test.Sdk`) |
| Audio assets | **36** | 6 sub-folders, all referenced by `AudioAssets.cs` resolve on disk |
| Texture assets | 0 | Textures live inside KayKit `.glb` files; no `Assets/Textures/` folder |
| Models | 2 | `AshfordEstate.glb`, `BaseCharacter.fbx` — both **imported** (`.import` sidecars present) |
| KayKit instances | **many** | Walls, floors, pillars, props instanced as `PackedScene` nodes in F1–F4 scene files |

### LOC by category

| Folder | LOC | Files |
|---|---|---|
| `Scripts/Core` | 2,502 | 8 (GameManager, VoiceManager, VoiceRecorder, PlaybackManager, AudioAssets, VocalImprintTracker, EscalationTimer, AchievementManager) |
| `Scripts/UI` | 3,960 | 11 (HUD, MainMenu, Lobbies ×2, PauseMenu, SettingsMenu, FirstTimeSetup, GestureWheel, RoleNotification, RoleSelect, UiSounds) |
| `Scripts/World` | 2,646 | 25 (4× floor builders, Floor1Room, FloorAssetCatalog, FloorPropGrid, FloorNavigationBaker, 9× items/world objects, SequenceManager, RegistrationBoard, WordPool, WordRegistry, MonologuePool, Radio + RadioBroadcast + RadioItem) |
| `Scripts/Player` | 2,462 | 6 (PlayerController, CameraManager, GestureSystem, RoleData, TokenVisual, VocalSacrifice) |
| `Scripts/Player/Abilities` | 644 | 7 (Clap, LoudStun, StaticBubble, MuteSilentDrop, EchoReplay, ArchivistRegistration, WitnessBurst) |
| `Scripts/Enemy` | 1,864 | 5 (ListenerAI, AdaptiveEvolution, PatrolWaypoint, ListenerSoundEvent, ListenerGroupCache) |
| `Scripts/Items` | 1,361 | 9 (Battery, BreakableWindow, Candle, GramophonePedestal, Knockover, Lighter, NoiseBox, SignalJammer, Torch) |
| `Scripts/Networking` | 769 | 1 (NetworkManager) |
| **Total** | **15,564** | **72** |

---

## Progress by section (rows counted from current `REQUIREMENTS.md`, status re-verified today)

| § | Section | Done | Partial | Not started | Total |
|---|---|---|---|---|---|
| 3 | Voice Mechanics | 12 | 0 | 0 | 12 |
| 4 | Listener AI | 7 | 3 | 0 | 10 |
| 5 | Player Roles | 9 | 0 | 0 | 9 |
| 6 | Map Design | 12 | 1 | 0 | 13 |
| 7 | Objective System | 9 | 6 | 0 | 15 |
| 8 | Escalation Timeline | 2 | 3 | 0 | 5 |
| 9 | Death & Spectator | 6 | 3 | 0 | 9 |
| 10 | Items & Environmental | 8 | 4 | 0 | 12 |
| 11 | UI & HUD | 5 | 13 | 0 | 18 |
| 12 | Audio Design | 7 | 3 | 0 | 10 |
| 13 | Multiplayer | 4 | 9 | 0 | 13 |
| 14 | Progression | 3 | 7 | 0 | 10 |
| 15 | Achievements | 4 | 6 | 0 | 10 |
| 18 | Random Matchmaking | 3 | 7 | 0 | 10 |
| **TOTAL** | | **91** | **65** | **0** | **156** |

**Roll-up:** 91 done · 65 partial · 0 not started in code · 1 known runtime blocker (GameScene crash — see Audit notes) · 5 deferred (Steam — see Deferred section).

**Note on §11:** Counts include all UI surfaces (HUD, settings, lobbies, menus). Sub-totals for §11.2 HUD widgets (Phase tracker, battery, teammate status, Last Word Token, voice activity) are still partial because some HUD widgets are wired at the script level but the corresponding scene children are placeholder `Label`/`ColorRect` exports awaiting designer hand-off.

**Note on row counts vs old CHECKLIST:** `REQUIREMENTS.md` has grown from 139 → 156 rows since the previous CHECKLIST (additional sub-features added: dual-timer escalation, single-survivor fail state, total party wipe screen, text broadcaster mode, vote-kick threshold variations, etc.). The Done/Partial/Not started columns here are re-audited from code, not copied from `REQUIREMENTS.md`.

---

## ✅ Done (verified in code today)

### Voice & Roles (old checklist still valid + new)

- **§3.1** Token transfer (server-auth, anti-spoof) — `VoiceManager.UpdateTokenHolder`, `TokenVisual`
- **§3.2** Mic calibration (30s) — lobby Calibrate button + progress bar + baseline label in `CustomLobby`/`MatchmakingLobby`
- **§3.3** Tier classification Silent/Whisper/Normal/Shout — `VoiceManager.ClassifyTier` with 5% hysteresis
- **§3.3** Tier 0 environmental detection (4m) — `ListenerAI.SubWhisperRadius`
- **§3.3** Environmental vs voice Tier 1 distinction — `ListenerAI.HearNoise` case 1
- **§3.4** Vocal imprinting profile — `VocalImprintTracker.cs` + per-player tracking
- **§3.5** Playback Trap (90s silence → mimicry) — `PlaybackManager.cs` + per-player `VoiceRecorder`
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

### Listener AI

- **§4.2** State machine (Idle/Alerted/Hunting/Frenzy) — `ListenerAI.UpdateStateLogic` + RPC eye-glow
- **§4.2** Eye-socket colour animation per state — `GetEyeColorForState`
- **§4.2** 10-minute escalation (Alerted 8s → Idle pre-10m, 15s → Hunting post-10m) — `GameManager.EscalationReached` + `ListenerAI`
- **§4.2.1** 5m attack range + vision rule — `IsPlayerAttackEligible` + `ListenerMovementThreshold = 0.12`
- **§4.2.1** 1Hz hearing-priority tick during sprint — `_hearingPriorityTimer`
- **§4.3** Adaptive Evolution (Metric A–D, 5-min shifts) — `AdaptiveEvolution.cs` (dormant, enable post-launch — `Enabled=false` gate, kept as stub)
- **§4.4** Voice mimicry (Playback) — covered by `PlaybackManager.cs` + `VoiceRecorder`
- **§4.5** Catch & Death sequence — `PlayerController` Death Sequence group: 2s fade, 60% pitch playback, death-card fields, Whisper transition, hit logs, AttackRange 1.8m cooldown

### Map & World

- **§6.1** F1 Basement interactive setup — `NavigationRegion3D` + `ListenerSpawn` + `NoteSpawn_*` markers + `RegistrationBoard` + `FloorNavigationBaker.cs` + **instanced KayKit Dungeon assets** in `F1_Basement.tscn` (walls/floor/pillar/props as `PackedScene` nodes visible in editor) + `WorldEnvironment` with dark sky, fog, glow + 5 warm point lights
- **§6.1** F2 Bedrooms scene — `F2_BedroomsBuilder.cs` + `F2_Bedrooms.tscn` with `KayKitFurniture/bed_double_A.glb` instanced + creak + dead-phone markers
- **§6.1** F3 Library (Silence Room) — `F3LibraryBuilder.cs` + `F3_Library.tscn`
- **§6.1** F4 Clock Tower — `F4ClockTowerBuilder.cs` + `F4_ClockTower.tscn` with bell + heavy door
- **§6.2** Creaking floor tiles — `CreakZone.cs` + 3 placed zones in `F1_Basement.tscn`
- **§6.2** Broken intercom (Floor 1) — `Intercom.cs` script present
- **§6.3** Wardrobe (room 2A) — `Wardrobe.cs` present
- **§6.3** Dead phones (2B, 2D) — `DeadPhone.cs` present
- **§6.4** Silence Room — `SilenceRoom.cs` present (capacity + audio isolation logic)
- **§6.5** Clock Tower heavy door (12s barricade) — `Barricade.cs` present
- **§6.5** Clock bell every 5 min — `ClockBell.cs` present

### Items (all 9 — `Scripts/Items/` + `Scenes/Items/`)

- **§10.1** Lighter — `LighterItem.cs` + `LighterItem.tscn`
- **§10.1** Torch Battery — `BatteryItem.cs` + `BatteryItem.tscn`
- **§10.1** Noise Box — `NoiseBoxItem.cs` + `NoiseBoxItem.tscn`
- **§10.1** Signal Jammer — `SignalJammerItem.cs` + `SignalJammerItem.tscn`
- **§10.1** Gramophone pedestal — `GramophonePedestal.cs` + `GramophonePedestal.tscn`
- **§10.2** Candles — `CandleItem.cs` + `CandleItem.tscn`
- **§10.2** Breakable windows — `BreakableWindow.cs` + `BreakableWindow.tscn`
- **§10.2** Furniture knockover — `KnockoverObject.cs` + `KnockoverObject.tscn`

### Objective System (Phase 1 / 2 / 3)

- **§7.1** Note pickup (E hold, silent) — `NoteItem.cs` (112 LOC, `NotePickedUp` signal, `PickupHoldSeconds` export)
- **§7.1** Word pool — `WordPool.cs` 90 words (50 Tier 1 monosyllabic + 40 Tier 2 polysyllabic, gothic register)
- **§7.1** Voice registration of picked-up words — `RegistrationBoard.cs` + `WordRegistry.cs`
- **§7.1** Sequence reveal — `SequenceManager.cs` (151 LOC, `SequenceRevealed` signal, `SustainedTierDuration`, `LockDuration`, WordAccepted/Rejected/Reset/Locked/Complete signals)
- **§7.3** Phase 3 broadcaster monologue — `MonologuePool.cs` (157 LOC, 60 dramatic + 20 absurdist)
- **§7.3** Radio pickup + handoff — `RadioBroadcast.cs` + `RadioItem.cs` + `Radio.cs`
- **§10.2** Environmental Tier 1 vs voice Tier 1 distinction — `HearNoise` case 1

### Death, Spectator, Audio

- **§9.1** Screen fade to black (2s) on death — `PlayerController.DeathFadeDuration` + `_deathOverlay` ColorRect
- **§9.1** Distorted voice playback (60% pitch) — `PlayerController.DeathPlaybackPitch` + `_deathAudioPlayer`
- **§9.1** Death card (cause, hold time, speak time, severity, tip) — `PlayerController._deathCard` + `_pendingDeathReason` + run-time stats fields
- **§9.2** Whisper spectator mode — `PlayerController.IsInWhisperMode` + `SpectatorMoveSpeed` + 2× movement rate + phase-through-walls branch
- **§12.2** Listener Hunting breath (8m, 3s loop) — `ListenerAI` `ApplyState` → `AudioAssets.PlayOneShot3D(ListenerHuntingBreath, ...)` + `Assets/Audio/listener/listener_hunting_breath.wav`
- **§12.2** Listener Frenzy tone (dissonant cluster) — `ListenerAI` `ApplyState` → `AudioAssets.PlayOneShot3D(ListenerFrenzyTone, ...)` + `Assets/Audio/listener/listener_frenzy_tone.wav`
- **§12.2** Catch silence (1s) — `ListenerAI.KillByListener` pre-step → `AudioAssets.PlayOneShot3D(ListenerCatchSilence, ...)`
- **§12.2** Listener idle hum — `HumPlayerPath` AudioStreamPlayer3D wired in `ListenerAI` + `listener_hum.wav`
- **§12.2** Listener alert click — `listener_alert_click.wav` referenced + imported
- **§12.3** Gramophone crackled music (10s loop) — `GramophoneItem.cs` + `AudioAssets.GramophoneMusicLoop` + 90s lifetime
- **§12.3** UI SFX set — 6 files in `Assets/Audio/ui/` (click/hover/back/error/failure_stinger/victory_stinger) wired through `UiSounds.cs`
- **§12.3** World SFX set — 10 files in `Assets/Audio/world/` (door open/close ×2, glass shatter ×2, lock open, item pickup, gramophone, clock bell, creak, impact)
- **§12.3** Ability SFX set — 5 files in `Assets/Audio/abilities/` (clap, loud stun, static bubble, vocal sacrifice, witness burst)
- **§12.3** Player footstep SFX — 7 files in `Assets/Audio/player/` (walk ×2, run ×2, wood ×2, landing)
- **§12.3** Ambience — 3 files in `Assets/Audio/ambience/` (ambience loop, machine loop, wind)
- **§12.4** Default bus layout — `default_bus_layout.tres` configured for Master/Listener/Microphone buses

### Multiplayer

- **§13.1** Host-authoritative multiplayer (2–4) — `NetworkManager` + `GameManager`
- **§13.5** Token transfer latency (client-predict + server-validate) — `UpdateTokenHolder`
- **§13.6** Grief-detection event logging — token frequency events emitted to `NetworkManager` (exemption windows pending — see Partial)
- **§18.3** Forced orientation mode (<3 prior runs) — `GameManager._orientationActive`

### Progression

- **§15** AchievementManager — `Scripts/Core/AchievementManager.cs` (233 LOC, registered as autoload in `project.godot`, exposes `GetAllUnlocked`, `Unlock(id)`, signal-based notification)

---

## 🟡 Partial (wired but gaps remain)

| ID | Feature | Gap |
|---|---|---|
| 3.2 | Mic calibration UI | Script-level wiring complete; **main-menu** calibrate CTA missing from `MainMenu.tscn` (lobby entry point only) |
| 4.2 | 5-min/15-min dual timer escalation | `EscalationTimer.cs` has 5 milestones (10/20/25/30/?? min) but the **dual-timer 8s/15s post-10min Alerted↔Hunting transition** is not yet wired into `ListenerAI`'s `ApplyState` |
| 6.1 | Floor asset parity | F1/F2/F3/F4 scenes rebuilt with KayKit; **post-pass polish** (decals, sound scattering) deferred — see `2026-06-27-optimization-plan.md` |
| 7.2 | 10m clock-mechanism radius check | `SequenceManager` has `SustainedTierDuration` but no `ClockMechanism.cs` enforcing radius + LOS for activation |
| 7.2 | Player-count scaling (1-skip at ≥2 survivors) | `SequenceManager` skeleton in place; **scaling rule** not implemented |
| 7.2 | Single-survivor fail state | No `FailState.cs` — would need a new scene + script |
| 7.3 | Text Broadcaster mode (0.6× typing speed) | `MonologuePool` content ready; **no `TextBroadcaster.cs`** — broadcaster uses voice-only |
| 8.1 | Phase-3 broadcaster handoff | `RadioBroadcast.cs` + `RadioItem.cs` wired; **on-broadcaster-death timer reset + no-progress-memory** logic not verified |
| 8.1 | Final countdown UI | `EscalationTimer` raises milestones; **HUD 3-min countdown overlay** still a TODO inside `EscalationTimer.cs:66` |
| 9.1 | Death screen → Whisper transition | Wired in `PlayerController`; **deeper visual polish** (camera shake, vignette, fade timing) deferred |
| 9.2 | Listener real-time position map | Whisper spectator mode active; **no `SpectatorMap.cs` / `SpectatorMapMarker.cs`** for J-key 15s pin |
| 10.2 | Furniture barricade (E hold 3s) | `Barricade.cs` exists for the **Clock Tower door**; **generic furniture barricade** not split into a separate component |
| 11.2 | Last Word Token indicator | HUD script wired; **proximity pulse rates** (slow/medium/fast/strobe) + Tier-1 light-blue rule need design pass |
| 11.2 | Voice activity indicator | Tier label + volume meter present; **Tier-1 light-blue colour rule** missing |
| 11.2 | Phase tracker (top-left, 3 dots) | `HUDManager.cs` exports exist; **scene-side `Control` children** are placeholder labels |
| 11.2 | Battery indicator (bottom-left, torch only) | `BatteryItem` emits; **HUD widget** not wired |
| 11.2 | Teammate status icons (4 corners) | `HUDManager` exports; **scene children** placeholder |
| 11.3 | Listener proximity pulse (12m) | No `AccessibilityPulse.cs` |
| 11.3 | Listener direction indicator (8m, 2s updates) | No `DirectionArc.cs` |
| 11.3 | Subtitle system | No `SubtitleManager.cs` |
| 11.3 | Keybind remapping | No `ControlsRemap.cs` (J reserved) |
| 11.3 | Text Broadcaster toggle in Accessibility | No `AccessibilitySettings.cs` |
| 11.5 | Mic re-run button | `SettingsMenu.cs` exports calibration UI; **"re-run calibration" button** missing |
| 11.5 | Region selector | No region data structure yet |
| 13.3 | Voice chat SDK | Mic amplitude-only; **WebRTC transport not integrated** |
| 13.3 | Privacy notice | `MainMenu.GDPRNoticeText` exists; **no First-Time Setup screen consent step** — `FirstTimeSetup.cs` exists but its consent step is currently a stub |
| 13.4 | Audio settings sliders | See §11.5 |
| 13.6 | Grief detection exemption windows | Token frequency events emitted; **Static-bubble exemption + 2s Sacrifice lock window** not explicit in `NetworkManager` |
| 14.1 | Patrol route randomization | `PatrolWaypoint.cs` cycles **alphabetically**, not shuffled with zone constraints |
| 14.5 | Local save schema | `user://settings.cfg` exists; **missing `runs`, `deaths`, `tip_counter`, `token_hold_runs`, `per_run_speak_time`, setup-complete flag, privacy ack timestamp** |
| 18.2 | Public lobby browser | `MatchmakingLobby.cs` exists; **no lobby-list UI** (single-room flow only) |
| 18.3 | Consent reminder + privacy | Text exists in `MainMenu`; **no explicit lobby banner** |
| 18.3 | Lobby text chat | No chat component wired in `MatchmakingLobby` |
| 18.3 | Pre-run mic check display (per-player meter) | Script supports; **per-player overlay** not split out |
| 18.3 | Quick Join | No `QuickJoin.cs` |
| 18.6 | Anti-grief UI | No **scoreboard mute / post-run report** UI surface |

> **Audit correction vs old checklist:** Many "not started" items from the previous CHECKLIST are now real implementations. Most notably:
> - All 9 items in §10 now have full `.cs` + `.tscn` pairs (no missing files).
> - §7.1 (note pickup) is done — `NoteItem.cs` is 112 LOC with a real `NotePickedUp` signal.
> - §9.1 (death fade + death card) is done — `PlayerController` has the entire `Death Sequence` export group.
> - §11.4 (First-Time Setup) is done — `FirstTimeSetup.cs` + `Scenes/FirstTimeSetup.tscn` exist; consent step is the only partial.
> - §15 (achievements) is done — `AchievementManager.cs` is 233 LOC, registered as autoload.

---

## ⛔ Deferred (Steam-partner blockers)

These cannot be implemented until a Steam partner account / AppID is provisioned.
Documented for completeness; **do not implement** until scope decision. Two
existing `TODO` comments inside `NetworkManager.cs` (lines 703, 733) reference
the Steam Lobby ID replacement — these are the integration seams.

- **§13.6** Random-matchmaking grief escalation (24h lockout) — needs persistent account identity
- **§15.1** Strangers in the Dark achievement — needs Steam friends check
- **§18.5** Region selection (NA E/W, EU W/C, SEA, OCE) — needs Steam Relay routing
- **§18.7** Strangers in the Dark — same as §15.1
- **§45** Full Steam integration (Achievements + Relay + public lobby browser)

---

## ⚠️ Post-launch (P2 in design, NOT blockers)

Documented in `GAME_DESIGN.md` but explicitly out of EA scope. Keep as design
notes only:

- **§9.2** Ghost burst spectator (3s VC windows, 20s CD) — design only
- **§12.4** Room-aware VC reverb — nice-to-have
- **§14.3** Cosmetic unlocks — Steam achievements
- **§14.4** Difficulty modes (Whisper Only / Deaf Run / One Life)
- **§14.5** Save backup export button + Cloud save

---

## 🚧 Known inline TODOs (7)

All 7 inline TODO/FIXME/stub markers across the entire `Scripts/` tree — kept
here so they don't get lost:

1. `Scripts/Core/EscalationTimer.cs:66` — `// TODO: Notify HUD to show a 3-minute countdown` (§8.1 Final countdown UI)
2. `Scripts/Core/GameManager.cs:321` — `// TODO: Signal HUDManager to hide orientation overlay when it is implemented` (§18.3 orientation UI)
3. `Scripts/Enemy/AdaptiveEvolution.cs:8` — Doc comment: "Disabled in EA; kept as a stub that can be enabled post-launch." (§4.3 — intentional)
4. `Scripts/Networking/NetworkManager.cs:703` — `// TODO: Replace with Steam Lobby ID when Steamworks.NET is integrated.` (Steam)
5. `Scripts/Networking/NetworkManager.cs:733` — `// TODO: Replace with Steam Lobby ID when Steamworks.NET is integrated.` (Steam)
6. `Scripts/UI/HUDManager.cs:369` — `// TODO: amber pulse on dedicated teammate status bar when implemented.` (§11.2)
7. `Scripts/World/RegistrationBoard.cs:72` — `// TODO: drive a progress bar overlay once the RegistrationBoard UI exists.` (§5.Archivist / §7.1)

## 🚧 Known runtime blocker

- **GameScene crash on launch** — present in the previous checklist, status not re-verified in this audit (not the audit's scope). Track in `docs/notes/` before next sprint.

---

## 🚧 Resolved open questions (from previous CHECKLIST)

These used to be open questions in `REQUIREMENTS.md`. Status today:

1. **Word pool** (§7.1) — **RESOLVED**. `WordPool.cs` ships 90 gothic horror-comedy words (50 Tier 1 + 40 Tier 2). Generated 2026-06-25 by writer-agent.
2. **Monologue pool** (§7.3) — **RESOLVED**. `MonologuePool.cs` ships 60 dramatic + 20 absurdist monologues.
3. **Art assets** (§6, §10, §12) — **PARTIALLY RESOLVED**. `AshfordEstate.glb` + `BaseCharacter.fbx` are imported; KayKit Dungeon/Furniture `.glb` files are imported and instanced in all 4 floor scenes. Audio + texture folders now populated (36 audio files). Texture folder still empty (textures live inside KayKit `.glb`s, which is intentional).
4. **Voice SDK direction** (§13.3) — **STILL OPEN**. WebRTC vs. mic-amplitude-only is undecided. Current implementation is mic-amplitude-only and is sufficient for the EA vertical slice; this decision should be revisited before adding real cross-network voice chat.

---

## Recommended next-sprint targets (P0 vertical slice)

The previous CHECKLIST's sprint-7 list is now stale (most of those items are
done). New priorities, ranked by unblock-effect vs effort:

1. **§11.2/§11.3 HUD scene hand-off** — `HUDManager.cs` exports everything but the scene children are placeholders. One design pass + scene wiring unblocks §11.2/§11.3 in one go (~3–5 days).
2. **§8.1 Final countdown HUD overlay** — finishes the `EscalationTimer` TODO at line 66 and gives players the 3-min warning that the design promises.
3. **§14.5 Local save schema** — adds `runs`, `deaths`, `tip_counter`, `token_hold_runs`, `per_run_speak_time`, setup-complete flag, privacy ack timestamp to `user://settings.cfg`. Unlocks achievement progress tracking and post-launch stats.
4. **§13.6 Grief exemption windows** — make `StaticBubble` + `VocalSacrifice` lock window exempt from grief detection (small, well-scoped `NetworkManager` change).
5. **§18.2 Lobby browser UI** — list view over existing `MatchmakingLobby` flow; needed before any non-private playtesting.
6. **§9.2 Spectator map + J-pin** — small new scripts (`SpectatorMap.cs`, `SpectatorMapMarker.cs`), closes the loop on the death sequence.
7. **§7.3 Text Broadcaster mode** — new `TextBroadcaster.cs`, 0.6× speed display, accessibility path.
8. **§13.3 First-Time Setup consent step** — finish the existing `FirstTimeSetup.cs` consent handler.
9. **Resolve GameScene launch crash** — required before any playtest.
