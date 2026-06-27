# Last Word — Feature Requirements Matrix

Generated from a full read of `GAME_DESIGN.md` (v2.5), `Listener.md`, `player.md`, and the current codebase.
Source-of-truth for what to build, in what order, and what blocks each item.

**Legend**
- **MVP?** — Yes / No (post-launch) / Optional (MVP if scoped, post-launch otherwise)
- **Status** — `Done` (in code, working) / `Partial` (some wiring, gaps remain) / `Not started` (zero code) / `Blocked` (depends on art / external SDK)
- **Where** — primary script(s) or scene(s); `new:` = file does not yet exist

---

## §3 Voice Mechanics

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 3.1 | Last Word Token system (server-authoritative transfer) | Yes | Done | `Scripts/Core/VoiceManager.cs`, `Scripts/Player/TokenVisual.cs` | Anti-spoof positions derived server-side. |
| 3.2 | Mic calibration flow (30s baseline capture) | Yes | Partial | `Scripts/Core/VoiceManager.cs` | `StartCalibration` + `ProcessCalibration` exist. No UI flow wired in `MainMenu`/`CustomLobby`/`MatchmakingLobby`. |
| 3.3 | Voice tier classification (Silent/Whisper/Normal/Shout) | Yes | Done | `Scripts/Core/VoiceManager.cs::ClassifyTier` | Hysteresis 5% buffer matches §3.3 dead-zone rule. |
| 3.3 | Tier 0 environmental detection (4m, Alerted only) | Yes | Done | `Scripts/Enemy/ListenerAI.cs::HearNoise` case 0 | SubWhisperRadius = 4m wired. |
| 3.3 | Tier 1 player-vs-environmental distinction | Yes | Partial | `Scripts/Enemy/ListenerAI.cs::HearNoise` case 1 | Player voice: orientation + faint blue eye glow (no pathfinding). **Environmental Tier 1** (Gramophone) navigates to source — implemented as Tier-1 fixed-position case but no Gramophone item exists yet. |
| 3.4 | Vocal imprinting profile per living player | No | Not started | new: `Scripts/Core/VocalImprintTracker.cs` | Design tracks cumulative speaking time 0–30s+ across tiers. Required for Second Listener targeting (§8). Dead profile decay 60s. |
| 3.5 | Playback Trap (90s silence → mimicry) | No | Not started | new: `Scripts/Core/PlaybackManager.cs` + clip storage | Microphone-API recording + distortion filter. Random matchmaking 10-min delay. |
| 3.6 | Vocal Sacrifice (G-hold 1s + loud speech) | Yes | Not started | new: `Scripts/Player/VocalSacrifice.cs` + UI overlay | 30s lock, pre-signal amber pulse, grief-detection on lock activation. |
| 3.7 | Gesture system (Z X C V B N M L) | Yes | Not started | new: `Scripts/Player/GestureSystem.cs` + 8 anim slots | Visible at 6m, no sound. Includes Mute bonus keys 1–5. |
| 3.7 | Radial gesture wheel (MMB hold 0.3s) | Yes | Not started | new: `Scripts/UI/GestureWheel.cs` | 8-segment radial overlay, 20-px deadzone. |
| 3.7 | Clap (Q key, post–Lights Out only, Tier 0) | Yes | Not started | new: `Scripts/Player/ClapAbility.cs` | 12s cooldown, 15% baseline, 4m detection. |
| 3.7 | Role ability keys (F Loud, R Static, T Echo) | Yes | Partial | F + R: new; T: post-launch | `Echo` (T) explicitly **post-launch**. |

---

## §4 The Listener AI

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 4.2 | State machine (Idle/Alerted/Hunting/Frenzy) | Yes | Done | `Scripts/Enemy/ListenerAI.cs::UpdateStateLogic` | Eye-glow color sync + RPC replication. |
| 4.2 | Eye-socket color animation per state | Yes | Done | `Scripts/Enemy/ListenerAI.cs::GetEyeColorForState` | Pulsing scale during Frenzy. |
| 4.2 | Alerted 8s reset / 15s Hunting transition (post-10min) | Yes | Partial | `Scripts/Enemy/ListenerAI.cs` — has `AlertSilenceTimeout` + `WhisperDecayCap` | **No 10-minute mark switch.** The dual-timer (§8) escalation hasn't been wired into `GameManager`. |
| 4.2.1 | 5m attack range (Token-agnostic, kills moving/noisy) | Yes | Done | `Scripts/Enemy/ListenerAI.cs::CheckProximitySensors` | Line-of-sight raycast + `IsPlayerAttackEligible` requires movement or recent noise. |
| 4.2.1 | Vision rule (stationary = safe in sight) | Yes | Done | `Scripts/Enemy/ListenerAI.cs::IsPlayerAttackEligible` | Movement threshold (`ListenerMovementThreshold = 0.12`) prevents camera jitter false positives. |
| 4.2.1 | Hearing priority tick (1Hz during sprint) | Yes | Done | `Scripts/Enemy/ListenerAI.cs::HandleMovementNoise` | `_hearingPriorityTimer` + `HearingPriorityInterval`. |
| 4.2.1 | Authoritative priority table | Yes | Done | `Scripts/Enemy/BT/ListenerBehaviorTree.tres` | 8-entry BTSelector. |
| 4.3 | Adaptive Evolution (Metric A–D) | No | Not started | new: `Scripts/Enemy/AdaptiveEvolution.cs` | Rule implemented as dormant for post-launch. |
| 4.4 | Voice mimicry (Playback) | No | Not started | covered by 3.5 | |
| 4.5 | Catch & Death sequence (grab + fade + voice playback + death card) | Yes | Partial | `Scripts/Player/PlayerController.cs::KillByListener` (RPC + ApplyListenerDeath) | **Missing:** screen fade (2s), recorded voice playback at 60% pitch, death card UI, transition to Whisper mode. |

---

## §5 Player Roles

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 5 | Role selection in lobby | Yes | Not started | new: `Scripts/UI/RoleSelect.cs` + lobby UI | No-duplicate rule. `No Role` option for >3 players. |
| 5.Mute | Passive: halved detection radius | Yes | Not started | new: `Scripts/Player/RoleData.cs` (mute modifier) | Hooks into `ListenerAI` radius checks per-player. |
| 5.Mute | Silent Drop (E on Registration Board, 2s) | Yes | Not started | new: `Scripts/World/RegistrationBoard.cs` | Floor 1 board non-functional (no notes spawn there). |
| 5.Mute | 5 bonus gestures (keys 1–5) | Yes | Not started | covered by 3.7 | |
| 5.Loud | Passive: speech always ≥ Tier 2 | Yes | Not started | new: `Scripts/Player/RoleData.cs` (loud modifier) | Hooks into `VoiceManager.ClassifyTier`. |
| 5.Loud | F stun pulse (5s, 90s cooldown, Tier 2.5) | Yes | Not started | new: `Scripts/Player/Abilities/LoudStun.cs` | +5s repositioning window post-stun. |
| 5.Static | R white-noise bubble (4m, 40s, 2 charges) | Yes | Not started | new: `Scripts/Player/Abilities/StaticBubble.cs` | Visible 15m / 20m post–Lights Out. Bubble collapses on death. |
| 5.Echo | T replay decoy (60s CD) | No | Not started | new: `Scripts/Player/Abilities/EchoReplay.cs` | **Post-launch.** T unbound in EA. |
| 5.Archivist | Silent note registration (5s hold) | No | Not started | new: post-launch | |
| 5.Witness | Extended ghost burst (6s vs 3s) + Listener path reveal | No | Not started | new: post-launch | |
| 5 | Role death notification ("F stun lost") | Yes | Not started | new: HUD overlay in `Scripts/UI/RoleNotification.cs` | |

---

## §6 Map Design — Ashford Estate

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 6.1 | Floor 1 Basement (greybox + spawn) | Yes | Not started | new: `Scenes/Floors/F1_Basement.tscn` + meshes in `Assets/` | Required first: validates Listener AI + nav-bake + Token loop. |
| 6.1 | Floor 2 Bedrooms | Yes | Not started | new: `Scenes/Floors/F2_Bedrooms.tscn` | Creak corridor, 2 of 4 notes, Registration Board, 2A wardrobe, 2B/2D phones. |
| 6.1 | Floor 3 Library | Yes | Not started | new: `Scenes/Floors/F3_Library.tscn` | Silence Room with 2-player cap + Aggressive Check + 2-player forced-exit. |
| 6.1 | Floor 4 Clock Tower | Yes | Not started | new: `Scenes/Floors/F4_ClockTower.tscn` | Spiral staircase, heavy barricadeable door (12s), bell ring every 5min, radio. |
| 6.2 | Creaking floor tiles (Tier 0, 4m) | Yes | Not started | new: `Scripts/World/CreakZone.cs` | Per-floor trigger zones. |
| 6.2 | Broken intercom (Floor 1) | Yes | Not started | new: `Scripts/World/Intercom.cs` | Floor-wide player perception, 20m Tier 2 at device position. |
| 6.3 | Wardrobe (room 2A, 20s mute, exit risk 5m) | Yes | Not started | new: `Scripts/World/Wardrobe.cs` | Listener opens during Hunting/Frenzy. |
| 6.3 | Dead phones (2B, 2D) — one-use hint | Yes | Not started | new: `Scripts/World/DeadPhone.cs` | UI teammate picker, Tier 2 event at phone position. |
| 6.4 | Silence Room (capacity 2, audio-isolated both ways) | Yes | Not started | new: `Scripts/World/SilenceRoom.cs` | + Aggressive Check (10m + 45s silence), 60s station / 20s 2-player. Break-through destroys door. |
| 6.4 | Broken intercom (Floor 3) | Yes | Not started | new: `Scripts/World/Intercom.cs` (same component, different scene) | |
| 6.5 | Clock Tower heavy door (12s barricade) | Yes | Not started | new: `Scripts/World/Barricade.cs` (shared component) | |
| 6.5 | Bell ring every 5min (4s mask window) | Yes | Not started | new: `Scripts/World/ClockBell.cs` | Mutes detection + audio. Token transfer unaffected. |
| 6.5 | Radio (Final Broadcast pickup/handoff) | Yes | Not started | new: `Scripts/World/RadioItem.cs` | 1s pickup delay, 2s grace window post-pickup, 10s monologue. |

---

## §7 Objective System

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 7.1 | Phase tracker HUD (3 dots, current glows) | Yes | Not started | new: `Scripts/UI/PhaseTracker.cs` | |
| 7.1 | Note pickup (E hold 2s, silent) | Yes | Not started | new: `Scripts/World/NoteItem.cs` + `Scripts/World/WordRegistry.cs` | Randomised spawn from 12 positions (6/floor). |
| 7.1 | Voice registration (Tier 1+, instant) | Yes | Not started | new: `Scripts/World/WordRegistry.cs` | 90-word pool (50 Tier-1 + 40 Tier-2) — **needs source data**, see Open Questions. |
| 7.1 | Mute Silent Drop at Registration Board | Yes | Partial | covered by 5.Mute (depends on board component) | |
| 7.1 | Sequence reveal (gated on registration complete) | Yes | Not started | new: `Scripts/World/SequenceManager.cs` | Randomized per run, host-stored. |
| 7.2 | 10m clock-mechanism radius check | Yes | Not started | new: `Scripts/World/ClockMechanism.cs` | Faint floor glow indicator. |
| 7.2 | Sustained Tier 2 for 0.5s word registration | Yes | Not started | new: `Scripts/World/SequenceManager.cs` | Continuous-amplitude check, not single-frame peak. |
| 7.2 | Wrong-order reset + 30s lock | Yes | Not started | new: `Scripts/World/SequenceManager.cs` | |
| 7.2 | Player-count scaling (lower of survivors / registered) | Yes | Not started | new: `Scripts/World/SequenceManager.cs` | No floor; 1-word skip allowed if ≥2 survivors. |
| 7.2 | Single-survivor fail state (10s grace) | Yes | Not started | new: `Scripts/UI/FailState.cs` | Session-size aware (2-player fails on first death). |
| 7.2 | Total party wipe screen | Yes | Not started | new: `Scripts/UI/WipeScreen.cs` | "The estate is silent. No voice remains." |
| 7.3 | Phase 3 broadcaster monologue (10s sustained Tier 2) | Yes | Not started | new: `Scripts/World/RadioBroadcast.cs` | Monologue pool 60 dramatic + 20 absurdist — **needs source data**. |
| 7.3 | Radio handoff on broadcaster death (timer reset, no progress memory) | Yes | Not started | new: `Scripts/World/RadioBroadcast.cs` | |
| 7.3 | Phase 3 Permanent Frenzy | Yes | Partial | `Scripts/Enemy/ListenerAI.cs::IsPhase3PermanentFrenzyActive` | Currently `virtual → false`. Hook to GameManager phase state. |
| 7.3 | Text Broadcaster mode (0.6× speed typing) | Yes | Not started | new: `Scripts/UI/TextBroadcaster.cs` | Phase 3 only. Phase 2 limitation disclosed in lobby. |

---

## §8 Escalation Timeline

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 8.1 | Minute-10 Silence ImmGodot (Alerted→Hunting 15s) | Yes | Not started | new: `Scripts/Core/EscalationTimer.cs` | Toggle ListenerAI behavior at 10-min mark. |
| 8.1 | Minute-20 Duplication (Second Listener spawn) | Yes | Not started | new: `Scripts/Enemy/ListenerAI.cs` second instance + `Scripts/Core/SpawnSecondListener.cs` | Falls back to Token holder if highest-speaker is dead. |
| 8.1 | Minute-25 Lights Out (fixed lighting off) | Yes | Not started | new: `Scripts/World/LightingController.cs` | Auto-grant standard torches to unequipped players. |
| 8.1 | Minute-30 Final Deadline (3-min countdown → 3rd Listener) | Yes | Not started | new: `Scripts/Core/EscalationTimer.cs` | Radio breaks if expired. |
| 8.2 | Pacing philosophy (system-level) | Yes | Done | (design doc only) | |

---

## §9 Death & Spectator

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 9.1 | Screen fade to black (2s) on death | Yes | Not started | new: `Scripts/UI/DeathFadeOverlay.cs` | |
| 9.1 | Distorted voice playback (60% pitch) | Yes | Not started | new: `Scripts/Core/DeathAudioPlayer.cs` | Requires clip recording first — currently absent (mic capture is amplitude-only). |
| 9.1 | Death card (cause, hold time, speak time, severity, tip) | Yes | Not started | new: `Scripts/UI/DeathCard.cs` | Tip suppression: first 5 deaths OR 3 Token-hold runs. |
| 9.1 | Total party wipe screen (4 cards side-by-side) | Yes | Not started | covered by 7.2 wipe screen | |
| 9.2 | Whisper mode (2× move, phase through walls) | Yes | Not started | new: `Scripts/Player/PlayerController.cs` dead-state branch | Excludes Listener collision (2m buffer + 5s auto-push to 3m). |
| 9.2 | Listener real-time position visibility (map) | Yes | Not started | new: `Scripts/UI/SpectatorMap.cs` | Full-estate schematic per floor; Tab to switch. |
| 9.2 | J-key map marker (15s, 1 active, no sound) | Yes | Not started | new: `Scripts/UI/SpectatorMapMarker.cs` | Cannot be remapped. |
| 9.2 | ESC overlay during spectator | Yes | Partial | `Scripts/UI/PauseMenu.cs` | Standard overlay works; J suspended during open needs explicit guard. |
| 9.2 | Ghost burst (3s VC windows, 20s CD) | No | Not started | post-launch | |

---

## §10 Items & Environmental Interactions

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 10.1 | Lighter (3min sustained light, auto-light candles at 1.5m) | Yes | Not started | new: `Scripts/World/Items/LighterItem.cs` | Held in hand; specific interactions exempt. |
| 10.1 | Torch Battery (90s replacement) | Yes | Not started | new: `Scripts/World/Items/TorchItem.cs` | 4 per run (one per floor). |
| 10.1 | Noise Box (Tier 0 pulses, 4s delay then 4s × 11) | Yes | Not started | new: `Scripts/World/Items/NoiseBox.cs` | 45s total. Alerted-only; Hunting escalation post-10min. |
| 10.1 | Signal Jammer (silences creaks 90s) | Yes | Not started | new: `Scripts/World/Items/SignalJammer.cs` | One-time use per run. |
| 10.1 | Torch Upgraded (4min, brighter, 10m visible) | Yes | Not started | covered by TorchItem | |
| 10.1 | Old Gramophone (Tier 1, 90s, environmental source) | Yes | Not started | new: `Scripts/World/Items/GramophoneItem.cs` | Fixed-position → Listener pathfinds. |
| 10.1 | Earmuffs (Playback immunity) | No | Not started | post-launch | Playback-dependent. |
| 10.2 | Furniture barricade (E hold 3s, 8s / 12s door) | Yes | Not started | new: `Scripts/World/Barricade.cs` | Silent. Furniture within 2m required. |
| 10.2 | Furniture knockover (Tier 0, 30% baseline) | Yes | Not started | new: `Scripts/World/KnockoverPhysics.cs` | Slightly larger hitbox than visual mesh. |
| 10.2 | Candles (auto-light 1.5m + pathfinding confusion 3–5s @ 70%) | Yes | Not started | new: `Scripts/World/Candle.cs` | Alerted state only. |
| 10.2 | Breakable windows (wind ambient after break) | Yes | Not started | new: `Scripts/World/Window.cs` | Ambient only, no Listener alert. |
| 10.2 | Environmental Tier 1 vs voice Tier 1 distinction | Yes | Done | `Scripts/Enemy/ListenerAI.cs::HearNoise` case 1 | |

---

## §11 UI & HUD

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 11.2 | Last Word Token indicator (top-centre, proximity pulse) | Yes | Partial | `Scripts/UI/HUDManager.cs` | Indicator exists; **missing proximity pulse rates** (slow>12m / medium 8–12m / fast 4–8m / strobe <4m). |
| 11.2 | Voice activity indicator (bottom-right, tier-colored) | Yes | Partial | `Scripts/UI/HUDManager.cs` | Tier label + volume meter present. **Missing**: light-blue Tier-1 color rule. |
| 11.2 | Phase tracker (top-left, 3 dots) | Yes | Not started | covered by 7.1 | |
| 11.2 | Battery indicator (bottom-left, only when torch equipped) | Yes | Not started | new: HUD component | |
| 11.2 | Teammate status icons (4 corners) | Yes | Not started | new: HUD component | alive/Token/dead/SilenceRoom/role-disc. Frenzy overrides for locked target. |
| 11.2 | Vocal Sacrifice countdown (amber) | Yes | Not started | new: HUD component (covered by 3.6) | |
| 11.2 | Phase 2 fail state indicator (red X + message) | Yes | Not started | covered by 7.2 | |
| 11.2 | Clap cooldown indicator (post-25min) | Yes | Not started | covered by 3.7 | |
| 11.3 | Listener proximity pulse (12m, accessibility) | Yes | Not started | new: `Scripts/UI/AccessibilityPulse.cs` | |
| 11.3 | Listener direction indicator (8m, 2s updates) | Yes | Not started | new: `Scripts/UI/DirectionArc.cs` | |
| 11.3 | Subtitle system (above bottom HUD row) | Yes | Not started | new: `Scripts/UI/SubtitleManager.cs` | Player VC excluded. |
| 11.3 | Keybind remapping screen | Yes | Not started | new: `Scripts/UI/ControlsRemap.cs` | J reserved. Hold-action compatibility warning. |
| 11.3 | Text Broadcaster mode toggle (Settings > Accessibility) | Yes | Not started | new: `Scripts/UI/AccessibilitySettings.cs` | Phase 2 limitation disclosed in description. |
| 11.4 | First-Time Setup screen (region/calibration/accessibility/keybinds/privacy) | Yes | Not started | new: `Scripts/UI/FirstTimeSetup.cs` | Mandatory first launch. |
| 11.5 | Settings menu (General/Audio/Privacy) | Yes | Partial | `Scripts/UI/SettingsMenu.cs` (file exists, content unknown) | Mic calibration re-run button needed. Region selection needed. Privacy "view notice" button needed. |
| 11.5 | Audio settings sliders (Master/VC/Listener ≥20%/Ambient/per-player) | Yes | Partial | `Scripts/UI/PauseMenu.cs` (likely) | Listener floor 20% + warning tooltip <30%. |
| 11.6 | Death card tip suppression (5 deaths OR 3 Token-hold runs) | Yes | Not started | covered by 9.1 | |
| 11.7 | Non-HUD feedback (token "you" sting, whisper vignette) | Yes | Not started | new: `Scripts/UI/FeedbackFx.cs` | |

---

## §12 Audio Design

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 12.2 | Listener idle hum (20Hz, 12m) | Yes | Done | `Scripts/Enemy/ListenerAI.cs` `HumPlayerPath` (AudioStreamPlayer3D wired) | `Assets/Audio/listener/listener_hum.wav` imported + loaded in `InitializeHumPlayer` (line ~1117). |
| 12.2 | Listener Alerted click | Yes | Done | `Scripts/Enemy/ListenerAI.cs` `ApplyState` (~line 1140) | `Assets/Audio/listener/listener_alert_click.wav` + `AudioAssets.ListenerAlertClick` const + plays on `→Alerted` transition. |
| 12.2 | Listener Hunting breath (8m) | Yes | Done | `Scripts/Enemy/ListenerAI.cs` `ApplyState` (~line 1147) | `Assets/Audio/listener/listener_hunting_breath.wav` + `AudioAssets.ListenerHuntingBreath` const + plays on `→Hunting` transition with pitch variance 0.95-1.05. |
| 12.2 | Listener Frenzy tone | Yes | Done | `Scripts/Enemy/ListenerAI.cs` `ApplyState` (~line 1154) | `Assets/Audio/listener/listener_frenzy_tone.wav` + `AudioAssets.ListenerFrenzyTone` const + plays on `→Frenzy` transition (scream lock / Phase-3) with pitch variance 0.92-1.08. |
| 12.2 | Catch silence (1s) | Yes | Done | `Scripts/Enemy/ListenerAI.cs` `KillByListener` pre-step (~line 1474) | `Assets/Audio/listener/listener_catch_silence.wav` + `AudioAssets.ListenerCatchSilence` const + plays on Listener attack kill before `controller.KillByListener(...)`. |
| 12.3 | Creaking floor audio (3D positional, 15m audibility) | Yes | Done | `Scripts/World/CreakZone.cs` (line ~50) | `Assets/Audio/world/creak.wav` + `AudioAssets.Creak` const + plays via `AudioAssets.PlayOneShot3D` on `CreakZone` overlap. |
| 12.3 | Clock bell (full-estate broadcast, 4s) | Yes | Done | `Scripts/World/ClockBell.cs` (line ~45) | `Assets/Audio/world/clock_bell.wav` + `AudioAssets.ClockBell` const + plays via `AudioAssets.PlayOneShot3D` on each `Ring()` (300s interval). |
| 12.3 | Broken window wind (persistent 5%) | Yes | Partial | `Scripts/Core/AudioAssets.cs` (WindAmbience const) | `Assets/Audio/ambience/wind_ambience.wav` asset + const registered. **Missing**: `Scripts/World/Window.cs` controller to drive playback on window break (covered by §10.2). |
| 12.3 | Gramophone crackled music | Yes | Partial | `Scripts/World/GramophoneItem.cs` (new) + `Scripts/Core/AudioAssets.cs` (GramophoneMusicLoop) | `Assets/Audio/world/gramophone_music_loop.wav` + 10s loopable vinyl crackle + 90s lifetime + Tier-1 environmental noise repeat every 15s. **Missing**: pickup / charge / inventory UI side of §10.1. |
| 12.4 | Room-aware VC reverb | No | Not started | post-launch / nice-to-have | |

---

## §13 Multiplayer

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 13.1 | Host-authoritative multiplayer (2–4 players) | Yes | Done | `Scripts/Networking/NetworkManager.cs` + `Scripts/Core/GameManager.cs` | |
| 13.1 | ESC overlay during live sessions (no pause) | Yes | Partial | `Scripts/UI/PauseMenu.cs` | Need verification: Token indicator active during ESC open. |
| 13.2 | Host disconnect handling (60s rejoin window) | Yes | Not started | new: `Scripts/Networking/HostDisconnectHandler.cs` | Per-player 60s slot reservation. Dead-at-disconnect stats on reconnect. **ENet-only** — Steam Relay reconnect path deferred. |
| 13.2 | Mic failure detection (>10s zero amplitude) | Yes | Not started | new: `Scripts/Core/MicFailureWatchdog.cs` | Auto-treat as Mute, no grief flag. |
| 13.2 | Involuntary vs voluntary disconnect distinction | Yes | Not started | covered by HostDisconnectHandler | Only voluntary leaves trigger abandonment cooldown. |
| 13.3 | Voice chat SDK integration | Yes | Partial | `Scripts/Core/VoiceManager.cs` | Currently mic-only amplitude (no WebRTC transport). **Decision needed**: integrate Godot Voice/WebRTC or accept per-process local mic + network-RPC amplitude sync. |
| 13.3 | Privacy notice (lobby + First-Time Setup) | Yes | Partial | `Scripts/UI/MainMenu.cs::GDPRNoticeText` | Text exists; First-Time Setup screen does not. |
| 13.4 | Audio settings sliders | Yes | Partial | `Scripts/UI/SettingsMenu.cs` | Listener floor 20% + warning tooltip <30%. |
| 13.5 | Token transfer latency (client-side prediction) | Yes | Done | `Scripts/Core/VoiceManager.cs::UpdateTokenHolder` | Already local-predict + server-validate. |
| 13.6 | Vote-kick (3-of-4 / 2-of-3 / 2-of-2) | Yes | Not started | new: `Scripts/Networking/VoteKick.cs` | Dead-player votes count. 2-player host unilateral once per session. |
| 13.6 | Grief detection logs (high-freq Token, Vocal Sacrifice misuse) | Yes | Partial | `Scripts/Core/VoiceManager.cs` has Token frequency events | Static bubble exemption rule needs explicit impl. 2-second Vocal Sacrifice lock-based window. |
| 13.6 | Session abandonment cooldown (10m/5m/0) | Yes | Not started | new: tracked in NetworkManager or PlayerInfo | Threshold = 20-min Duplication event. |
| 13.6 | Random matchmaking grief escalation (2 kicks in 24h → 2h lockout) | No | Deferred | new: requires persistent account identity (Steam) | **Deferred** — needs account identity. Local ENet sessions use host-boundary only. |

---

## §14 Progression & Replayability

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 14.1 | Note location randomization (12 positions, 6/floor) | Yes | Not started | covered by 7.1 | |
| 14.1 | Word sequence randomization per run | Yes | Not started | covered by 7.1 | |
| 14.1 | Monologue randomization (75% dramatic / 25% absurdist) | Yes | Not started | covered by 7.3 | |
| 14.1 | Item location randomization (8 per floor) | Yes | Not started | covered by 10.1 | |
| 14.1 | Listener patrol route randomization | Yes | Partial | `Scripts/Enemy/PatrolWaypoint.cs` (alphabetical, not random) | Currently sequential; needs shuffle + zone constraints. |
| 14.2 | Post-run stats screen (words, longest silence, transfers, sacrifices, death-state) | Yes | Not started | new: `Scripts/UI/RunStatsScreen.cs` | Neutral per-player; no leaderboard. |
| 14.3 | Cosmetic unlocks (Token/Listener/Player/Death-card) | No | Not started | post-launch | All from Steam achievements. |
| 14.4 | Difficulty modes (Whisper Only / Deaf Run / One Life) | No | Not started | post-launch | |
| 14.5 | Local save schema (runs, deaths, tip counter, Token-hold runs, per-run speak time, cosmetics, prefs) | Yes | Partial | `user://settings.cfg` exists in `NetworkManager.LoadMatchmakerConfig` + `VoiceManager.LoadBaseline` | **Missing**: `runs`, `deaths`, `tip_counter`, `token_hold_runs`, `per_run_speak_time` fields. Setup-completion flag. Privacy ack timestamp. |
| 14.5 | Save backup export button | No | Not started | post-launch | Cloud save also post-launch. |

---

## §15 Achievements & Viral Hooks

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 15.1 | Golden Silence | Yes | Not started | tracked in local save | Archivist exclusion (post-launch). |
| 15.1 | No Screaming | Yes | Not started | tracked in local save | |
| 15.1 | Last Words (broadcaster + Token) | Yes | Not started | tracked in local save | |
| 15.1 | Final Broadcast (no pause) | Yes | Not started | tracked in local save | |
| 15.1 | Hot Potato (20 transfers/run, sacrifice-excluded) | Yes | Not started | tracked in local save | |
| 15.1 | The Sacrifice (Vocal Sacrifice + survive 30s) | Yes | Not started | covered by 3.6 | |
| 15.1 | Listener's Favourite (5 consecutive highest-speaker) | Yes | Not started | requires 3.4 imprint tracker | |
| 15.1 | Heard Nothing (no pre-Phase-3 Frenzy, no sacrifice) | Yes | Not started | tracked in local save | |
| 15.1 | Strangers in the Dark (random matchmaking, no friends) | No | Deferred | requires Steam friend status | **Deferred** — no Steam partner account. Re-evaluate when Steam is back in scope. |
| 15.1 | Gaslight (Playback trap kill) | No | Not started | post-launch | Playback-dependent. |

---

## §18 Random Matchmaking

| ID | Feature | MVP? | Status | Where | Notes / Blockers |
|---|---|---|---|---|---|
| 18.2 | Public lobby browser UI (host/players/region/status/role-slots/Text-Broadcaster) | Yes | Partial | `Scripts/UI/MatchmakingLobby.cs` (file exists) | **Missing**: lobby list / browser UI. Currently only single-room flow. **Without Steam Lobby**: replace public-lobby-list with direct-IP-code-only flow (§18.1 still supports private code sessions for "strangers" via code sharing). |
| 18.2 | Quick Join (region adjacency fallback) | Yes | Not started | new: `Scripts/Networking/QuickJoin.cs` | NA↔NA↔EU↔EU; SEA↔OCE↔NAW. No direct EU↔SEA. |
| 18.3 | Forced orientation mode (<3 prior runs) | Yes | Done | `Scripts/Core/GameManager.cs::_orientationActive` | Wired. |
| 18.3 | Lobby text chat | Yes | Not started | new: lobby-scoped chat | Closes on run start. |
| 18.3 | Pre-run mic check display (per-player meter) | Yes | Not started | new: lobby UI component | |
| 18.3 | Consent reminder + privacy notice | Yes | Partial | `Scripts/UI/MainMenu.cs::GDPRNoticeText` | Needs explicit lobby-screen banner. |
| 18.5 | Region selection (NA E/W, EU W/C, SEA, OCE) | No | Deferred | new: needs Steam Relay routing | **Deferred** — Steam Relay not in scope. Local ENet on `127.0.0.1` only. |
| 18.5 | Latency display + 150ms warning | Yes | Not started | new: lobby UI | |
| 18.6 | Anti-grief: lobby lock, scoreboard mute, post-run report | Yes | Not started | new: covered by 13.6 + UI | |
| 18.7 | Strangers in the Dark achievement | No | Deferred | covered by 15.1 | **Deferred** — requires Steam friends check. No Steam partner account in scope. |

---

## Summary

| Section | Done | Partial | Not started | Blocked | Total |
|---|---|---|---|---|---|
| §3 Voice | 3 | 2 | 7 | 0 | 12 |
| §4 Listener | 4 | 1 | 2 | 0 | 7 |
| §5 Roles | 0 | 0 | 12 | 0 | 12 |
| §6 Map | 0 | 0 | 14 | 0 | 14 |
| §7 Objectives | 0 | 1 | 11 | 0 | 12 |
| §8 Escalation | 0 | 0 | 4 | 0 | 4 |
| §9 Death/Spec | 0 | 1 | 6 | 0 | 7 |
| §10 Items | 0 | 0 | 11 | 0 | 11 |
| §11 UI | 0 | 4 | 13 | 0 | 17 |
| §12 Audio | 5 | 2 | 2 | 0 | 9 |
| §13 Multiplayer | 1 | 4 | 5 | 0 | 10 |
| §14 Progression | 0 | 1 | 7 | 0 | 8 |
| §15 Achievements | 0 | 0 | 8 | 1 | 9 |
| §18 Matchmaking | 0 | 2 | 5 | 0 | 7 |
| **TOTAL** | **13** | **19** | **103** | **0** | **139** (4 items moved to Deferred above) |

**13/139 features are fully implemented. 19 are partial. 103 are not started. 1 is blocked on Steam integration.**

---

## Deferred (out of scope — Steam partnership)

The following items are deferred until Steam partner account / AppID is provisioned:

- §13.6 Random matchmaking grief escalation (24h lockout) — needs persistent account identity
- §15.1 Strangers in the Dark achievement — needs Steam friends check
- §18.5 Region selection — needs Steam Relay routing
- §18.7 Strangers in the Dark — same as §15.1

**Net effect on scope:** the project ships with private-IP-code sessions and ENet listen-server only. Public lobby browser is replaced by direct-IP-code-sharing workflow (§18.1 still permits 1-player-starts-solo in private rooms for testing).

## Open Questions Blocking Scope

1. **Word pool** (§7.1) — design says 90 words in a separate asset file. Source data needed before Phase 1 can be tested.
2. **Monologue pool** (§7.3) — 60 dramatic + 20 absurdist. Source data needed before Phase 3 can be tested.
3. **Voice SDK direction** (§13.3) — Godot Voice/WebRTC integration vs. keep current mic-amplitude-only design?
4. **Art assets** (§6, §10, §12) — KayKit / Kenney downloaded yet, or greybox-only first?
