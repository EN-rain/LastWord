# Systems Index

## Systems

| ID | System | Priority | Status | Owner Script(s) | Blocker / Notes |
|---|---|---|---|---|---|
| S01 | VoiceManager — Token transfer & server-authoritative mic tracking | P0 | Done | `Scripts/Core/VoiceManager.cs`, `Scripts/Player/TokenVisual.cs` | Anti-spoof positions derived server-side |
| S02 | ListenerAI — State machine (Idle/Alerted/Hunting/Frenzy) | P0 | Done | `Scripts/Enemy/ListenerAI.cs::UpdateStateLogic` | Eye-glow color sync + RPC replication |
| S03 | NoteItem — Phase 1 coded-word pickup | P0 | Not started | new: `Scripts/World/NoteItem.cs` | Randomised spawn from 12 positions (6/floor); E hold 2s |
| S04 | WordRegistry — Voice registration of found words | P0 | Not started | new: `Scripts/World/WordRegistry.cs` | 90-word pool (50 Tier-1 + 40 Tier-2) — needs source data |
| S05 | RegistrationBoard — Mute Silent Drop / non-Mute word registration | P0 | Not started | new: `Scripts/World/RegistrationBoard.cs` | Floor 1 board non-functional (no notes spawn there) |
| S06 | SequenceManager — Phase 2 sequence reveal & word-order gate | P0 | Not started | new: `Scripts/World/SequenceManager.cs` | Host-stored, randomised per run |
| S07 | RadioBroadcast — Phase 3 broadcaster monologue & handoff | P0 | Not started | new: `Scripts/World/RadioBroadcast.cs` | 10s sustained Tier 2; monologue pool needs source data |
| S08 | GameManager — Host-authoritative session & phase state | P0 | Done | `Scripts/Core/GameManager.cs` | Runs Listener AI on host; `_orientationActive` wired |
| S09 | HUDManager — Token indicator, voice activity meter, phase tracker | P0 | Partial | `Scripts/UI/HUDManager.cs` | Missing Token proximity pulse rates (slow/medium/fast/strobe) and Tier-1 light-blue color |
| S10 | Death sequence — Grab, fade, voice playback, death card | P0 | Partial | `Scripts/Player/PlayerController.cs::KillByListener` | Missing screen fade (2s), recorded voice playback at 60% pitch, death card UI, transition to Whisper mode |
| S11 | Mic calibration — 30s baseline capture & per-device persistence | P1 | Partial | `Scripts/Core/VoiceManager.cs` | `StartCalibration` + `ProcessCalibration` exist; no UI flow wired in lobby screens yet |
| S12 | Voice tier classification — Silent/Whisper/Normal/Shout with hysteresis | P1 | Done | `Scripts/Core/VoiceManager.cs::ClassifyTier` | Hysteresis 5% buffer matches dead-zone rule |
| S13 | Mute role — Halved detection radius, Silent Drop, bonus gestures 1–5 | P1 | Not started | new: `Scripts/Player/RoleData.cs` (mute modifier) + `Scripts/UI/RoleSelect.cs` | Hooks into ListenerAI radius checks per-player |
| S14 | Loud role — Passive always ≥Tier 2, F stun pulse (5s, 90s CD) | P1 | Not started | new: `Scripts/Player/RoleData.cs` (loud modifier) + `Scripts/Player/Abilities/LoudStun.cs` | Hooks into `VoiceManager.ClassifyTier` |
| S15 | Static role — R white-noise bubble (4m, 40s, 2 charges) | P1 | Not started | new: `Scripts/Player/Abilities/StaticBubble.cs` | Visible 15m (20m post–Lights Out); bubble collapses on death |
| S16 | Floor 1 Basement — Greybox, spawn, creak tiles, broken intercom | P1 | Not started | new: `Scenes/Floors/F1_Basement.tscn` + meshes in `Assets/` | Validates Listener AI + nav-bake + Token loop |
| S17 | Floor 2 Bedrooms — Creak corridor, notes, wardrobe, dead phones | P1 | Not started | new: `Scenes/Floors/F2_Bedrooms.tscn` | 2 of 4 notes; Registration Board; rooms 2B/2D phones |
| S18 | Floor 3 Library — Silence Room, bookshelves, broken intercom | P1 | Not started | new: `Scenes/Floors/F3_Library.tscn` | Silence Room: 2-player cap, Aggressive Check, forced-exit |
| S19 | Floor 4 Clock Tower — Spiral stairs, heavy door, radio, bell | P1 | Not started | new: `Scenes/Floors/F4_ClockTower.tscn` | No Registration Board; Phase 1 expected complete before ascent |
| S20 | Escalation timeline — 10/20/25/30 minute events | P1 | Not started | new: `Scripts/Core/EscalationTimer.cs` + `Scripts/World/LightingController.cs` | Duplication at 20m, Lights Out at 25m, Final Deadline at 30m |
| S21 | Creaking floor tiles — Tier 0 environmental sound (4m) | P1 | Not started | new: `Scripts/World/CreakZone.cs` | Per-floor trigger zones |
| S22 | Broken intercom — Floor-wide player broadcast, Tier 2 at device | P1 | Not started | new: `Scripts/World/Intercom.cs` | 20m Tier 2 at device position |
| S23 | Wardrobe (room 2A) — 20s mute hide, exit risk 5m | P1 | Not started | new: `Scripts/World/Wardrobe.cs` | Listener opens during Hunting/Frenzy |
| S24 | Dead phones (2B, 2D) — One-use teammate hint via UI picker | P1 | Not started | new: `Scripts/World/DeadPhone.cs` | Tier 2 event at phone position |
| S25 | Silence Room — Audio isolation both ways, 2-player cap, Aggressive Check | P1 | Not started | new: `Scripts/World/SilenceRoom.cs` | 60s station / 20s 2-player; break-through destroys door |
| S26 | Clock bell — 5min mask window (4s), full-estate audio | P1 | Not started | new: `Scripts/World/ClockBell.cs` | Mutes detection + audio; Token transfer unaffected |
| S27 | Barricade system — Furniture barricade (8s/12s door) | P1 | Not started | new: `Scripts/World/Barricade.cs` | Silent; furniture within 2m required |
| S28 | Whisper spectator mode — Death sequence → 2× move, map, J-key marker | P1 | Not started | new: `Scripts/Player/PlayerController.cs` dead-state branch + `Scripts/UI/SpectatorMap.cs` | 2m Listener buffer + 5s auto-push to 3m |
| S29 | Settings menu — General/Audio/Privacy + keybind remapping | P1 | Partial | `Scripts/UI/SettingsMenu.cs`, `Scripts/UI/PauseMenu.cs`, `Scripts/UI/ControlsRemap.cs` | File exists; missing mic re-run, region, privacy view buttons |
| S30 | Vote-kick — Host unilateral (2-player) or majority threshold | P1 | Not started | new: `Scripts/Networking/VoteKick.cs` | Dead-player votes count |
| S31 | Post-run stats screen — Neutral per-player stats, no leaderboard | P1 | Not started | new: `Scripts/UI/RunStatsScreen.cs` | Tracks words, longest silence, transfers, sacrifices, death-state |
| S32 | Vocal Sacrifice — G-hold 1s + loud speech, 30s lock, amber HUD | P1 | Not started | new: `Scripts/Player/VocalSacrifice.cs` + HUD overlay | Grief detection: 2-second post-lock window |
| S33 | Gesture system — 8 standard gestures (Z X C V B N M L) + radial wheel | P1 | Not started | new: `Scripts/Player/GestureSystem.cs` + `Scripts/UI/GestureWheel.cs` | Visible at 6m, no sound; MMB hold 0.3s radial overlay, 20px deadzone |
| S34 | Clap ability — Q key, post–Lights Out only, Tier 0, 12s CD | P1 | Not started | new: `Scripts/Player/ClapAbility.cs` | 15% baseline, 4m detection; 0.5s room illumination |
| S35 | Item pool — Lighter, Torch Battery, Noise Box, Signal Jammer, Gramophone | P1 | Not started | new: `Scripts/World/Items/LighterItem.cs`, `TorchItem.cs`, `NoiseBox.cs`, `SignalJammer.cs`, `GramophoneItem.cs` | 8 spawn positions per floor; one battery guaranteed per floor |
| S36 | First-Time Setup — Region, calibration, accessibility, keybinds, privacy | P1 | Not started | new: `Scripts/UI/FirstTimeSetup.cs` | Mandatory first launch; stored in local save |
| S37 | Death card — Cause, Token hold time, speak time, severity, tip | P1 | Not started | new: `Scripts/UI/DeathCard.cs` | Tip suppressed after 5 deaths OR 3 Token-hold runs |
| S38 | Echo role — T replay decoy (60s CD) | P2 | Not started | new: `Scripts/Player/Abilities/EchoReplay.cs` | T unbound in EA; highest skill ceiling |
| S39 | Archivist role — Silent note registration (5s hold), shimmer vision | P2 | Not started | new: post-launch | Must still speak Phase 2 word aloud |
| S40 | Witness role — Extended ghost burst (6s vs 3s) + Listener path reveal | P2 | Not started | new: post-launch | Ghost burst spectator also post-launch |
| S41 | Vocal imprinting — Cumulative speaking-time profile per living player | P2 | Not started | new: `Scripts/Core/VocalImprintTracker.cs` | Required for Second Listener targeting; dead profile decay 60s |
| S42 | Playback Trap — 90s silence → mimicry (distorted + undistorted clips) | P2 | Not started | new: `Scripts/Core/PlaybackManager.cs` + clip storage | Microphone API recording + distortion filter; random matchmaking 10-min delay |
| S43 | Adaptive Evolution — Metric A–D, 5-minute interval parameter shifts | P2 | Not started | new: `Scripts/Enemy/AdaptiveEvolution.cs` | Rule implemented as dormant for post-launch |
| S44 | Ghost burst spectator — 3s VC windows, 20s CD, static signature | P2 | Not started | post-launch | 10% mic-volume equivalent; Listener perceives as sound |
| S45 | Steam integration — Achievements, Relay, public lobby browser | P2 | Deferred | new: requires Steam partner account / AppID | Public lobby browser replaced by direct-IP-code-only flow |

## Priority Legend
- **P0**: Required for first playable vertical slice (Floor 1 core loop)
- **P1**: Required for MVP Early Access
- **P2**: Post-launch / deferred

## Status Legend
- **Done**: Implemented and wired
- **Partial**: Some implementation, gaps remain
- **Not started**: Zero code
- **Deferred**: Explicitly out of MVP scope
