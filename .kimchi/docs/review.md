# Voice Mechanics Implementation Review

**Date:** 2026-06-27
**Scope:** MVP Voice Mechanics per `VOICE_MECHANICS_SPEC.md`, `REQUIREMENTS.md` §3, `GAME_DESIGN.md` §3.2/§3.6/§3.7.
**Constraint:** Read-only review; no source changes. dotnet CLI unavailable, so build verification was skipped.

## Verdict: NEEDS_FIXES

The gesture system skeleton is in place and well-structured, but it is not wired into the player scene or HUD. The remaining MVP features (mic calibration lobby flow, Vocal Sacrifice, Clap, Loud/Static role abilities, radial wheel integration) are either missing entirely or only stubbed. Several critical integration bugs would prevent the implemented gesture code from functioning in-game.

---

## Feature Status Table

| Feature | Spec Ref | Status | Files / Notes |
|---|---|---|---|
| 3.2 Mic calibration UI flow (Settings menu) | VOICE_MECHANICS_SPEC §1, GAME_DESIGN §3.2 | Partial | `Scripts/UI/SettingsMenu.cs` wires button/progress/status and saves baseline. |
| 3.2 Mic calibration UI flow (Lobby) | VOICE_MECHANICS_SPEC §1 | Not implemented | `CustomLobby.cs`, `MatchmakingLobby.cs`, and their scenes have no calibration CTA or progress bar. |
| 3.2 Calibration duration | VOICE_MECHANICS_SPEC §1, GAME_DESIGN §3.2 | Wrong value | `Scripts/Core/VoiceManager.cs` line 42 sets `CalibrationDuration = 3.0f`; spec requires 30 seconds. |
| 3.6 Vocal Sacrifice | VOICE_MECHANICS_SPEC §4, GAME_DESIGN §3.6 | Not implemented | `VocalSacrifice.cs` does not exist; `ListenerAI.cs` only has a stub returning `false` for `IsVocalSacrificeLockActive()`. |
| 3.7 Gesture system (8 keys Z-X-C-V-B-N-M-L) | VOICE_MECHANICS_SPEC §2, GAME_DESIGN §3.7 | Partial code, not wired | `Scripts/Player/GestureSystem.cs` and `PlayerController.cs` input routing exist, but `Scenes/Player.tscn` has no `GestureSystem` node. |
| 3.7 Radial gesture wheel (MMB hold 0.3s) | VOICE_MECHANICS_SPEC §2, GAME_DESIGN §3.7 | Partial code, not wired | `Scripts/UI/GestureWheel.cs` and `Scenes/GestureWheel.tscn` exist, but `Scenes/HUD.tscn` does not instantiate the wheel and `HUDManager.GestureWheelPath` is unassigned. |
| 3.7 Clap ability (Q, post-Lights Out) | VOICE_MECHANICS_SPEC §3, GAME_DESIGN §3.7 | Not implemented | `ClapAbility.cs` does not exist; no Lights Out integration in `GameManager` or `PlayerController`. |
| 3.7 Role ability F (Loud stun) | VOICE_MECHANICS_SPEC §5, GAME_DESIGN §5 | Not implemented | `RoleData.cs` and `LoudStun.cs` do not exist. |
| 3.7 Role ability R (Static bubble) | VOICE_MECHANICS_SPEC §5, GAME_DESIGN §5 | Not implemented | `StaticBubble.cs` does not exist. |
| 3.7 Role ability T (Echo replay) | Spec | Correctly skipped | Explicitly post-launch / out of scope; not present. |

---

## Critical Issues

### 1. Input actions required by the spec are missing from `project.godot`
**File:** `/mnt/c/Users/LENOVO/Documents/Last-word-godot/project.godot`

The project configuration contains no `[input]` section. `SettingsMenu.EnsureDefaultActions()` creates actions at runtime, but only when the Settings menu is opened. Gesture keys, `clap_q`, `sacrifice_g`, `radial_wheel_mmb`, and movement keys will fail in any scene where the settings menu has not yet run (e.g., directly launching `GameScene.tscn` for testing, or after the first boot if GDPR/setup flow changes). Additionally, the spec-required actions `ability_f` and `ability_r` are **not** created by `EnsureDefaultActions()`.

`Scripts/UI/SettingsMenu.cs` lines 218-237:
```csharp
private readonly Dictionary<string, string> _actionNames = new()
{
    ...
    { "clap_q", "Clap Action (Key Q)" },
    { "sacrifice_g", "Vocal Sacrifice (Key G)" },
    ...
};
```
The `_actionNames` dictionary lists `clap_q` and `sacrifice_g`, but the `EnsureDefaultActions()` method (same file, lines 240-265) does not register `ability_f` or `ability_r`.

**Suggested fix:** Add an `[input]` section to `project.godot` declaring all required actions (`move_*`, `gesture_*`, `clap_q`, `sacrifice_g`, `ability_f`, `ability_r`, `radial_wheel_mmb`, `spectator_j`), or extend `EnsureDefaultActions()` to register `ability_f`/`ability_r` and ensure it runs from an autoload before gameplay input is read.

---

### 2. GestureSystem node missing from Player scene
**File:** `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scenes/Player.tscn`

`PlayerController._Ready()` resolves the gesture system with:
```csharp
_gestureSystem = GetNodeOrNull<GestureSystem>("GestureSystem");
```
`Scripts/Player/PlayerController.cs` line 100.

However, `Scenes/Player.tscn` contains no `GestureSystem` child node. The gesture key handling block at lines 232-241 will therefore never call `PlayGesture()`. Even if the node were added later, `GestureSystem._Ready()` uses `Owner?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")`, which will fail because the `AnimationPlayer` is nested under `BaseCharacter`, not a direct child of the `Player` owner.

**Suggested fix:** Add a `GestureSystem` node to `Scenes/Player.tscn` as a child of the root `Player` node, and point `GestureSystem._Ready()` to search the player hierarchy recursively for the `AnimationPlayer` (similar to `PlayerController.FindAnimationPlayer()`).

---

### 3. GestureWheel not added to HUD scene
**File:** `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scenes/HUD.tscn`

`HUDManager._Ready()` attempts to resolve:
```csharp
_gestureWheel = GetNodeOrNull<GestureWheel>(GestureWheelPath);
```
`Scripts/UI/HUDManager.cs` line 55.

`Scenes/HUD.tscn` does not contain a `GestureWheel` instance, and the exported `GestureWheelPath` is unassigned in the scene. The radial wheel can therefore never open, and its `GestureSelected` signal is never connected to `GestureSystem.PlayGesture()`.

**Suggested fix:** Instantiate `Scenes/GestureWheel.tscn` under `HUD` in `Scenes/HUD.tscn`, assign it to `HUDManager.GestureWheelPath`, and connect `GestureWheel.GestureSelected` to `GestureSystem.PlayGesture()` (likely via `PlayerController` or a small coordinator script).

---

### 4. Calibration duration is 3 seconds instead of 30
**File:** `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scripts/Core/VoiceManager.cs` line 42

```csharp
[Export] public float CalibrationDuration = 3.0f;
```

The spec (`VOICE_MECHANICS_SPEC.md` §1, `GAME_DESIGN.md` §3.2) mandates a 30-second calibration step. The current value produces a 3-second calibration, which does not match the design intent and will yield an inaccurate baseline.

**Suggested fix:** Change `CalibrationDuration` to `30.0f`.

---

### 5. Lobby calibration UI missing
**Files:**
- `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scripts/UI/CustomLobby.cs`
- `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scripts/UI/MatchmakingLobby.cs`
- `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scenes/CustomLobby.tscn`
- `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scenes/MatchmakingLobby.tscn`

Both lobby scripts display live mic meters but have no "Calibrate Mic" button, progress bar, or baseline label. The spec requires host and clients to calibrate before ready-up, and the Ready/Start button to be disabled while `VoiceManager.Instance.IsCalibrating` is true. None of this wiring exists.

**Suggested fix:** Add a Calibrate button + ProgressBar + status label to both lobby scenes, connect them to `VoiceManager.StartCalibration()` / `CalibrationProgress` / `CalibrationFinished`, and disable the ready/start buttons during calibration.

---

### 6. Vocal Sacrifice is only a stub in the Listener AI
**File:** `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scripts/Enemy/ListenerAI.cs` lines 606-609

```csharp
protected virtual bool IsVocalSacrificeLockActive() => false;
protected virtual Node3D GetSecondListenerImprintTarget() => null;
protected virtual bool IsAudioSuppressedByFutureSystems(ListenerSoundEvent soundEvent) => false;
```

The behavior tree (`Scripts/Enemy/BT/ListenerBehaviorTree.tres`) includes a "Vocal Sacrifice" sequence that sets target mode 5, but `UpdateStateLogic()` in `ListenerAI.cs` has no `case ListenerTargetMode.VocalSacrifice:`, so the AI does nothing when that mode is selected. There is no `VocalSacrifice.cs`, no G-hold input handling in `PlayerController`, no activation-speech hook in `VoiceManager`, no HUD countdown, and no grief-detection window.

**Suggested fix:** Implement `Scripts/Player/Abilities/VocalSacrifice.cs`, wire G-hold in `PlayerController`, add the activation hook to `VoiceManager` tier detection, add the `VocalSacrifice` case to `ListenerAI.UpdateStateLogic()`, and expose HUD methods for the countdown / amber pulse.

---

### 7. Clap ability, Loud stun, and Static bubble are entirely absent
**Files:** None exist.

The following spec-required files were not created:
- `Scripts/Player/Abilities/ClapAbility.cs`
- `Scripts/Player/Abilities/LoudStun.cs`
- `Scripts/Player/Abilities/StaticBubble.cs`
- `Scripts/Player/RoleData.cs`

Consequently, Q, F, and R do nothing. `PlayerController` does not route these keys, `ListenerAI` has no stun state or audio-suppression override, and no role assignment exists.

**Suggested fix:** Create the four scripts above, add ability nodes to `Scenes/Player.tscn`, route Q/F/R input in `PlayerController`, implement the Listener stun and Static-bubble suppression hooks, and add HUD cooldown/charge indicators.

---

### 8. Calibration does not save baseline on finish inside `VoiceManager`
**File:** `/mnt/c/Users/LENOVO/Documents/Last-word-godot/Scripts/Core/VoiceManager.cs`

`ProcessCalibration()` emits `CalibrationFinished` but does not itself persist the baseline. Persistence currently relies on `SettingsMenu.OnCalibrationFinished()` calling `SaveSettings()`. This works for the settings menu but means lobby calibration (once added) would need duplicate save logic. More importantly, there is no `SaveBaseline()` counterpart to `LoadBaseline()`.

**Suggested fix:** Add a `SaveBaseline()` method in `VoiceManager` and call it from `ProcessCalibration()` so baseline persistence is centralized.

---

## Out-of-Scope Features Implemented

The following features are **not** in the Voice Mechanics spec but were added by the previous builder and should be flagged so they do not distract from MVP verification:

| Feature | Files | Notes |
|---|---|---|
| Phase 2/3 objective system (WordRegistry, SequenceManager, RadioBroadcast, RadioItem) | `Scripts/World/WordRegistry.cs`, `Scripts/Core/GameManager.cs`, `Scripts/UI/HUDManager.cs`, `Scenes/GameScene.tscn` | `HUDManager` references `WordRegistry` and `WordListLabelPath`; these nodes do not exist in `Scenes/HUD.tscn`, which will produce null-reference warnings at runtime. |
| Ashford Estate level geometry / floors | `Assets/Models/AshfordEstate.*`, `Scenes/Floors/` | Greybox art import; not related to voice mechanics. |
| Whisper spectator / death fade | `Scripts/Player/PlayerController.cs`, `Scenes/Player.tscn` | Death sequence and spectator movement were added but are outside the current voice-mechanics scope. |
| Matchmaking lobby logic | `Scripts/Networking/NetworkManager.cs`, `Scripts/UI/MatchmakingLobby.cs` | Expanded matchmaking queue/room-code logic; not part of the voice mechanics task. |

---

## File Inventory

### New files relevant to Voice Mechanics
- `Scripts/Player/GestureSystem.cs`
- `Scripts/UI/GestureWheel.cs`
- `Scenes/GestureWheel.tscn`

### New files outside Voice Mechanics scope
- `Assets/Models/AshfordEstate.*`
- `Scenes/Floors/`
- `Scripts/World/` (WordRegistry, etc.)
- `Tests/SequenceManagerTests.cs`
- `claude-code-game-studios/`, `design/`, `docs/`

### Modified files
- `-LastWord.csproj` — added NUnit test packages.
- `Scenes/GameScene.tscn` — added estate model, second Listener, navigation, TokenVisual.
- `Scenes/Player.tscn` — added death overlay/card nodes; **missing GestureSystem node**.
- `Scripts/Core/GameManager.cs` — added phase/objective scaffolding.
- `Scripts/Core/VoiceManager.cs` — calibration exists but duration wrong; no sacrifice hook.
- `Scripts/Enemy/BT/ListenerBehaviorTree.tres` — added VocalSacrifice target mode node.
- `Scripts/Enemy/ListenerAI.cs` — added Phase3 frenzy, stubs for sacrifice/static suppression.
- `Scripts/Networking/NetworkManager.cs` — expanded matchmaking.
- `Scripts/Player/CameraManager.cs` — whitespace/indentation change only.
- `Scripts/Player/PlayerController.cs` — gesture key routing, death/spectator sequence.
- `Scripts/Player/TokenVisual.cs` — whitespace/indentation change only.
- `Scripts/UI/HUDManager.cs` — added WordRegistry/gesture references; **missing scene nodes**.

### Unchanged files (relevant to spec)
- `Scenes/CustomLobby.tscn`
- `Scenes/MatchmakingLobby.tscn`
- `project.godot` — no input map entries.

---

## Recommended Next Build Steps

1. **Fix input map.** Add all required actions to `project.godot` (or an autoload) so gesture/ability keys work without first opening Settings.
2. **Wire the gesture system.** Add `GestureSystem` to `Scenes/Player.tscn`, fix its `AnimationPlayer` lookup, and add the `GestureWheel` instance to `Scenes/HUD.tscn` with signal connection.
3. **Correct calibration duration.** Set `VoiceManager.CalibrationDuration = 30.0f`.
4. **Add lobby calibration UI.** Add Calibrate button + progress bar to both lobby scenes and disable Ready during calibration.
5. **Implement missing abilities.** Create `ClapAbility`, `VocalSacrifice`, `LoudStun`, `StaticBubble`, and `RoleData`; route Q/G/F/R in `PlayerController`; extend `ListenerAI` for stun, sacrifice lock, and bubble suppression.
6. **Add centralized baseline save.** Implement `VoiceManager.SaveBaseline()` and call it on calibration completion.
7. **Build and run smoke tests** (`dotnet build LastWord.sln`) once the dotnet environment is available; verify gesture keys do not error and Q/G/F/R are gated correctly by role/Lights Out state.
