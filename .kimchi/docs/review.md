# UI Implementation Review

**Date:** 2026-06-28  
**Scope:** Godot 4.6 UI behavior and project UI code rules.  
**References:** `claude-code-game-studios/docs/engine-reference/godot/modules/ui.md`, `claude-code-game-studios/.claude/rules/ui-code.md`.

## Verdict: NEEDS_UI_POLISH

The UI is now functionally wired: main menu, first-time setup, settings, lobbies, HUD, pause menu, and gesture wheel all exist. The old review findings about missing gesture wheel, lobby calibration UI, and absent ability scripts are stale.

The remaining issues are quality and standards gaps: localization, Godot 4.6 focus/gamepad behavior, accessibility preferences, and stricter UI/game-state boundaries.

## Findings

### 1. Hardcoded User-Facing Text Is Widespread

**Severity:** High  
**Files:** `Scenes/*.tscn`, `Scripts/UI/*.cs`

Most visible UI text is literal English strings in scene files or C# assignments. This violates the UI rule that all UI text must go through localization.

Examples:
- `Scenes/HUD.tscn`: `Voice: Silent`, `ROOM CODE: OFFLINE`, debug labels.
- `Scripts/UI/FirstTimeSetup.cs`: setup titles, warnings, privacy notice, buttons.
- `Scripts/UI/SettingsMenu.cs`: status labels, dialogs, keybind labels.
- `Scripts/UI/CustomLobby.cs` and `MatchmakingLobby.cs`: lobby notices, room code labels, ready states.

**Fix:** Add localization keys and use `tr()`/Godot translation resources for all visible strings, including dynamic templates.

### 2. Godot 4.6 Focus Behavior Needs A Full Pass

**Severity:** High  
**Files:** all interactive UI scenes

Godot 4.6 separates mouse/touch focus from keyboard/gamepad focus. Current UI has buttons and controls, but there is no explicit project-wide focus policy, no documented focus-neighbor pass, and no validation that every screen works by keyboard/gamepad alone.

**Fix:** Set initial focus per screen, define focus neighbors where layout is non-linear, and test mouse and gamepad/keyboard paths separately.

### 3. Gesture Wheel Is Mouse-Centric

**Severity:** Medium  
**Files:** `Scripts/UI/GestureWheel.cs`, `Scenes/GestureWheel.tscn`

The gesture wheel opens via `radial_wheel_mmb` and selects by mouse position. It does not provide a keyboard/gamepad selection path, so it does not satisfy the all-input-methods rule.

**Fix:** Add keyboard/gamepad segment selection and confirm/cancel behavior.

### 4. Accessibility Preferences Are Partial

**Severity:** Medium  
**Files:** `Scripts/UI/SettingsMenu.cs`, `Scripts/UI/FirstTimeSetup.cs`, `Scripts/UI/HUDManager.cs`

Subtitles and proximity pulse are present, but required scalable text, colorblind modes, and reduced-motion behavior are not fully wired.

**Fix:** Add global text scale, colorblind palette, and reduced-motion settings; apply them through theme/UI update helpers.

### 5. UI Directly Mutates Or Requests Game State In Several Places

**Severity:** Medium  
**Files:** `Scripts/UI/MainMenu.cs`, `CustomLobby.cs`, `MatchmakingLobby.cs`, `SettingsMenu.cs`, `FirstTimeSetup.cs`

The UI currently calls `NetworkManager`, `VoiceManager`, `ConfigFile`, and scene changes directly. This works, but the UI rule says UI should display state and emit commands/events rather than own game state changes.

**Fix:** Introduce small command methods/signals for network actions, calibration, settings persistence, and scene transitions where practical.

## Cleared Stale Findings

- Gesture wheel is present in `Scenes/HUD.tscn` and assigned to `HUDManager.GestureWheelPath`.
- Lobby calibration UI exists in both lobby scripts.
- Ability actions `ability_f`, `ability_r`, and `ability_t` are present in settings.
- `VocalSacrifice`, `ClapAbility`, `LoudStun`, and `StaticBubble` files exist.
- Current build and Godot MCP startup were verified clean in the prior repair pass.
