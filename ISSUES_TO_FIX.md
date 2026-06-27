# Issues To Fix

Delete the whole `ISSUES_TO_FIX.md` file if all issues are fixed.

This file collects the non-security audit findings raised in this session, plus the attached note from another agent's optimization-progress attempt. It is intentionally a repair tracker, not a design document.

## Quick Checklist

- [ ] Clear stale `testhost.exe` PID `18136` or reboot, then rerun `dotnet build -v:minimal`.
- [ ] Rerun `dotnet test -v:minimal`; previous run timed out after 120 seconds and left the assembly locked.
- [ ] Rerun Godot MCP startup after the locked assembly is released to confirm the deferred `MainMenu` setup transition is clean.
- [ ] Fix remaining objective correctness gaps: Phase 1 spoken-word verification, Phase 2 auto-solve, empty sequence completion, and no-token Phase 3 targeting.
- [ ] Finish remaining role/ability gaps: `RoleSelect` final duplicate guard, Mute passive completeness, Vocal Sacrifice late `VoiceManager` subscription, Clap unused tuning, Static visibility range, Echo decoy listener baiting, Witness burst.
- [ ] Finish remaining audio/system gaps: playback trap enable/reporting, `VoiceRecorder` effect removal by instance, dev key override configurability, achievements, adaptive evolution, imprint death profiles.
- [ ] Resolve scene/content/doc drift: RegistrationBoard interaction radius, DeadPhone hint assignments, second Listener scene spawn semantics, settings input drift, monologue count, asset recheck, and stale docs.

Current detailed tracker count: 47 fixed items, 51 open audit items. The quick checklist above adds 7 summary action items. Delete this whole file only when every open item is resolved or intentionally removed from scope.

## Current Verification Notes

- `dotnet build -v:minimal` passed after the first repair batch, before `dotnet test` left a stuck `testhost.exe`.
- `dotnet test -v:minimal` timed out after 120 seconds and left PID `18136` holding `.godot/mono/temp/bin/Debug/-LastWord.dll`; `Stop-Process` and `taskkill /F` both returned access denied.
- `dotnet msbuild -t:CoreCompile -v:minimal` passes after the latest edits, so the current C# syntax compiles even though the normal build copy step is blocked by the stale test host.
- Godot MCP is working. `get_project_info` reports Godot `4.6.2.stable.mono.official.71f334935`, and `run_project` starts the project.
- The previous Godot startup error from `UiSounds.WireButton()` disconnecting nonexistent button signals was fixed.
- The next Godot startup error from `MainMenu._Ready()` changing scene immediately was fixed by deferring the scene change; it still needs a fresh run after PID `18136` releases the locked assembly.

## Build And Runtime Blockers

- [x] Project does not compile. `dotnet build -v:minimal` failed with errors in `Scripts/UI/FirstTimeSetup.cs` and `Scripts/UI/HUDManager.cs`.
  - `FirstTimeSetup.cs`: `AlignmentMode` was not found at multiple lines.
  - `FirstTimeSetup.cs`: `ThemeOverrideFontSizes`, `ThemeOverrideConstants`, and `ThemeOverrideColors` members were not found on the used controls.
  - `HUDManager.cs`: switch pattern constants fail because `TokenSlowDistance`, `TokenMediumDistance`, and `TokenFastDistance` are not compile-time constants.

- [x] Godot runtime reports an autoload failure for `AchievementManager`.
  - Godot MCP worked and returned project info.
  - Running the project failed with: `Failed to instantiate an autoload, script 'res://Scripts/Core/AchievementManager.cs' does not inherit from 'Node'.`
  - Source does inherit `Node`, so this is likely caused by the current C# build failure or stale assembly state.

- [x] `UiSounds.WireButton()` caused Godot runtime errors by disconnecting nonexistent button signal connections.
  - Replaced speculative event removal with metadata-based idempotent wiring.

- [x] `MainMenu._Ready()` changed scenes immediately while the scene tree was still adding/removing children.
  - Deferred the first-time setup scene transition with `SceneTree.ChangeSceneToFile`.

- [ ] Tests still need investigation.
  - `Tests/SequenceManagerTests.cs` exists, but the compile errors block test execution.
  - Latest result: `dotnet test -v:minimal` timed out after 120 seconds and left a stuck `testhost.exe` locking the Godot assembly.

## Scene And Node Wiring

- [x] Hardcoded `/root/GameManager` paths do not match the current scene tree.
  - `Scenes/GameScene.tscn` root node is `Main`, not `GameManager`.
  - Affected call sites include `HUDManager`, `PlayerController`, and `Radio`.

- [ ] Main game scene does not wire the expected Phase 1/2/3 nodes.
  - `GameManager` expects nodes such as `F1_Basement/SpawnPoints`, `WordRegistry`, `RegistrationBoard`, `RadioItemScene`, and `RadioSpawnMarkerPath`.
  - Current `Scenes/GameScene.tscn` primarily instances `AshfordEstate.glb` and does not appear to provide those nodes.

- [x] `GameScene.tscn` root `Main` has `visible = false`.
  - This can hide 3D descendants such as the estate, player, and listener.

- [x] `SpawnNoteItems` can null-reference `spawnRoot`.
  - If `spawnRoot` is missing but fallback markers are found, notes are still added through `spawnRoot.AddChild(note)`.

- [x] `WordRegistry` is required for progression but is optional and not created as a fallback.
  - `GameManager` subscribes to a configured registry or `WordRegistry.Instance`, but unlike `SequenceManager` and `RadioBroadcast`, it does not create one if missing.

- [x] `RegistrationBoard` silently no-ops if its exported `WordRegistry` is missing.
  - It calls `WordRegistry?.RegisterWord(...)` and has no fallback to `WordRegistry.Instance`.
  - `Scenes/RegistrationBoard.tscn` does not assign `WordRegistry`.

- [ ] `RegistrationBoard` interaction radius is misleading.
  - The scene has a child `InteractionZone`, but the script is on the root `Area3D` and listens to the root `BodyEntered`.
  - The child interaction sphere is unused.

- [x] `PlayerController` death voice playback is not wired in `Scenes/Player.tscn`.
  - The scene contains `VoiceRecorder` and `DeathAudioPlayer`, but exported `VoiceRecorderPath` and `DeathAudioPlayerPath` are not assigned.

- [x] Movement input actions are not guaranteed at boot.
  - `project.godot` has no `[input]` map.
  - `VoiceManager.EnsureInputActions()` creates gesture and ability actions, but not `move_forward`, `move_backward`, `move_left`, `move_right`, or `move_jump`.
  - `PlayerController` reads those actions directly.

## Objective Progression

- [x] Phase 1 note pickup appears unreachable.
  - `NoteItem.PickUp()` exists, but no caller was found.
  - `NoteItem.InteractionTrigger` is exported but not wired in `_Ready()`.

- [ ] Phase 1 registration does not verify the spoken word.
  - `RegistrationBoard` registers the held note when voice tier changes to whisper+, regardless of whether the player actually said the note word.

- [x] Offline Phase 1 registration is likely blocked.
  - Offline token transfer sets `_tokenHolder` but not `_tokenHolderPeerId`.
  - `RegistrationBoard` rejects peer IDs `<= 0`.

- [ ] Phase 2 sequence auto-solves without speech recognition.
  - `PlayerController` feeds the next expected word directly into `SequenceManager` whenever the player speaks at Normal tier.

- [ ] Empty Phase 2 sequences cannot complete.
  - `SequenceManager.GenerateSequence()` can generate an empty sequence for zero words or zero players.
  - `ValidateWord()` returns when `_currentIndex >= _sequence.Count` without setting `IsComplete`.

- [x] Phase 3 radio path is disconnected.
  - `GameManager` spawns `RadioItem`.
  - `RadioItem` only emits pickup.
  - The carried/broadcast `Radio` logic is separate and appears unused.
  - No code connects `RadioPickedUp` to create/attach a carried radio or start `RadioBroadcast`.

- [x] `RadioItem` cannot be picked up unless `InteractionTrigger` is assigned.
  - It creates a default mesh, but not a default `Area3D` trigger.

- [x] `RadioItem.ExecutePickup()` queues free before emitting `RadioPickedUp`.
  - `QueueFree()` is deferred, so it may still work in the same frame, but it is fragile if listeners expect node state.

- [x] `OnFinalBroadcastTransmitted()` duplicates victory flow and skips victory audio.
  - `OnBroadcastComplete()` plays `VictoryStinger`.
  - `OnFinalBroadcastTransmitted()` sets victory directly without the stinger.

- [x] `RadioBroadcast` final broadcast can only progress if a held `Radio` exists under the player.
  - The currently spawned `RadioItem` path does not appear to create that held radio.

## Roles And Abilities

- [x] Custom lobby role selector is not initialized.
  - `SetupRoleSelector()` exists in `CustomLobby`, but `_Ready()` never calls it.
  - `Scenes/CustomLobby.tscn` has an `OptionRole` with no predefined items.

- [x] No active duplicate-role enforcement in the lobby flow.
  - Design says duplicate roles are not allowed.
  - Live lobby role updates simply assign `SelectedRole`.
  - `RoleSelect.cs` has guard logic but appears unused by scenes.

- [x] Invalid role IDs can be stored.
  - `NetworkManager.UpdatePlayerRoleRPC()` casts raw ints to `PlayerRole` without range validation.

- [ ] `RoleSelect` can confirm a role that becomes taken.
  - It emits confirm without a final duplicate-role guard.

- [x] Mute and Archivist board abilities are unreachable.
  - `MuteSilentDrop` and `ArchivistRegistration` require `OnEnterBoard()` / `OnExitBoard()`.
  - `RegistrationBoard` never calls those methods.

- [x] Mute/Archivist silent registration depends on `RegistrationBoard` overlap and missing board callbacks.
  - Even if ability scripts process input, `_currentBoard` is never assigned.

- [ ] Mute detection passive is only partly applied.
  - Listener detection multiplier checks `RoleData.IsMute`, but Mute-specific puzzle/drop flow is not wired.

- [x] Loud Stun comment and behavior disagree.
  - Code comment says the ability transfers the Token.
  - Implementation reports a listener noise event but does not invoke token transfer.

- [x] Vocal Sacrifice never arms activation speech.
  - `_awaitingActivationSpeech` is required by `OnTierChanged()`.
  - The hold/pre-signal path never sets it to `true`.

- [ ] Vocal Sacrifice signal subscription may miss `VoiceManager`.
  - It subscribes only if `VoiceManager.Instance` exists during `_Ready()`.

- [x] Clap ability is permanently disabled unless missing code toggles it.
  - `ClapAbility.LightsOutActive` gates execution.
  - No code was found that sets it to `true`.

- [ ] `ClapAbility.SoundRadius` and `SoundBaselinePercent` are unused.
  - `ExecuteClap()` reports a fixed tier-0 noise event.

- [x] Static Bubble duplicates entries in `ActiveBubbles`.
  - `Deploy()` adds `this`.
  - `SyncDeploy()` also adds `this` with `CallLocal = true`.

- [ ] Static Bubble exported visibility ranges are unused.
  - `VisibleRange` and `VisibleRangePostLightsOut` are present but not consumed.

- [x] EchoReplay is callable even though there is no Echo role.
  - `PlayerRole` has no Echo entry.
  - Every player scene has `EchoReplay`.
  - `PlayerController` calls it on `ability_t` without a role check.

- [ ] EchoReplay decoy only plays audio locally in-world.
  - It does not report a noise event to `VoiceManager` or `ListenerAI`, so it may not bait the Listener.

- [ ] Witness burst is incomplete.
  - `WitnessGhostBurstDuration` and `WitnessBurst.GhostBurstDuration` are not applied to the death/spectator transition.
  - `TryRevealListenerPath()` has a TODO and returns success without rendering a path.

- [ ] Gesture visibility distance is unused.
  - `GestureSystem.MaxVisibilityDistance` is exported but gestures replicate globally without distance filtering.

- [x] Radial gesture wheel emits but is not connected to gameplay.
  - `GestureWheel` emits `GestureSelected`.
  - `HUDManager` only stores the wheel node.
  - No connection to `GestureSystem.PlayGesture()` was found.

## Audio, Voice, Noise, And Listener Behavior

- [x] Silence room and wardrobe flags are set but not consumed.
  - `PlayerController.IsAudioIsolated` and `IsSilenced` are toggled.
  - Sound emission/detection paths do not check them.

- [x] `PlayerController.EmitFootstepNoise()` and `EmitLandingNoise()` ignore `IsSilenced` / `IsAudioIsolated`.

- [x] `VoiceManager.DispatchNoiseToListeners()` ignores `IsSilenced` / `IsAudioIsolated`.

- [x] `CreakZone.CanBeSilencedByJammer` is unused.
  - No `SignalJammer` implementation was found.

- [x] Clock bell mask window is unused.
  - `ClockBell` exposes `IsMaskActive` and emits `BellRang`.
  - No listener/noise logic reads either.

- [ ] Playback trap is disabled by default and never enabled.
  - `PlaybackManager.TriggerEnabled = false`.
  - `GameManager` creates `PlaybackManager` but does not enable it.

- [ ] Playback mimicry does not notify Listener AI.
  - It spawns an `AudioStreamPlayer3D`, but does not report a noise event.

- [ ] `PlaybackManager.FindRandomLivingPlayer()` uses `GD.RandRange(0, living.Count - 1)` as a list index.
  - This is suspicious in C# because list indices must be ints.
  - Verify after the build is green.

- [x] `VoiceRecorder.CycleSegment()` can null-reference.
  - It calls `clip.GetLength()` before checking whether `clip` is null.

- [ ] `VoiceRecorder` removes audio bus effects by stored index.
  - Multiple player instances can add effects to the same bus.
  - Removing by an old index can remove the wrong effect or fail after indices shift.

- [x] `VoiceManager.GetLocalMinimumTier()` uses the first node in group `Player`.
  - In multiplayer this may not be the local authority player, so Loud passive tier logic can apply from the wrong player.

- [ ] `VoiceManager` dev keyboard voice overrides use raw `Key1`, `Key2`, `Key3`.
  - This is a hardcoded debug path and not exposed through input settings.

## World Interactions

- [x] DeadPhone and Intercom require interact to already be held on body entry.
  - If the player enters the area and then presses interact, nothing happens.

- [ ] DeadPhone hints default to static unless `PossibleHints` is assigned.
  - No scene assignment was confirmed.

- [x] Barricade logic is not wired.
  - `SetHolder()`, `ClearHolder()`, and `StartBreak()` exist, but no callers were found.

- [x] `Barricade.StartBreak()` does not schedule completion.
  - It logs/plays audio, but does not wait `BreakTime` and call `FinishBreak()`.

- [x] Wardrobe suppression expires after a timer, but actual sound paths do not honor `IsSilenced`.

- [x] Silence room isolation does not suppress incoming audio or outgoing noise in the current audio/listener code.

## Game State, Achievements, And Systems

- [ ] Second Listener is present from scene start instead of spawning at the 20-minute Duplication event.
  - `Scenes/GameScene.tscn` instances both `Listener1` and `Listener2` immediately.
  - The design says the second Listener spawns at minute 20 in the Basement.

- [x] Second Listener imprint targeting is defined but not implemented.
  - `ListenerTargetMode.SecondListenerImprint` exists.
  - The behavior tree can set target mode `7`.
  - `ListenerAI.UpdateStateLogic()` has no `case ListenerTargetMode.SecondListenerImprint`.

- [x] `GetSecondListenerImprintTarget()` always returns null.
  - `VocalImprintTracker` exists and records speaking time.
  - `ListenerAI` never queries it for an actual second-listener target.

- [x] 20-minute, 25-minute, and 30-minute escalation events are missing.
  - `GameManager` implements the 10-minute escalation.
  - No implementation was found for minute-20 second Listener spawn, minute-25 Lights Out, or minute-30 third Listener/radio-break countdown.

- [x] The second Listener currently uses the same scene/script configuration as the first Listener.
  - There is no scene flag, exported mode, subclass, or runtime setup that marks `Listener2` as the imprint-targeting clone.

- [ ] `AchievementManager` likely misses `GameManager` signals.
  - It subscribes to `GameManager.Instance` only during its own `_Ready()`.
  - As an autoload, it can initialize before the game scene creates `GameManager`.

- [ ] Several achievements are only stubbed or partially wired.
  - `WrongNumber` has a TODO hook.
  - `TheLastWord` uses `SecondsSinceLastVoice <= 5.0f` rather than actual broadcaster/final-word identity.
  - `MarathonOfSilence` checks local role on victory but does not verify full-match silence.

- [ ] `AdaptiveEvolution` is created but has no effect.
  - It defaults disabled.
  - No listener code reads its speed/hearing/attack multipliers.

- [ ] `VocalImprintTracker.MarkDead()` only marks existing profiles.
  - If a player dies without a profile, no dead profile is created.

- [ ] No-token Phase 3 targeting depends on finding a node in group `RadioItem`.
  - The design says no-token Phase 3 should target the radio physical location.
  - If the radio item has been picked up/freed or converted into a different carried node, targeting can fail.

- [ ] `ListenerAI` Phase 3 permanent frenzy ignores all noise, which matches some design constraints, but should be verified against the final radio/no-token rules.

## UI And Settings

- [x] First-time setup can complete without real calibration.
  - Finish is gated by privacy acceptance, not calibration completion.
  - If `SettingsMenuScene` is missing, calibration is marked completed.

- [x] First-time setup design says mic calibration is non-skippable, but implementation allows completion without it.

- [ ] Settings/default input definitions drift.
  - `VoiceManager` creates `ability_f`, `ability_r`, and `ability_t`.
  - `SettingsMenu` default keybind dictionary omits those ability actions.

- [ ] `SettingsMenu.EnsureDefaultActions()` includes `move_sprint`, but `PlayerController` movement code does not appear to use `move_sprint`.

- [ ] Custom lobby and matchmaking lobby role selection use `OptionButton` item index as role value.
  - This is fragile if item order changes.

- [ ] Main menu first-time setup integration exists.
  - Earlier review corrected this: `MainMenu` does gate to `FirstTimeSetup` when `setup_complete` is false.
  - Do not treat first-time setup as completely unintegrated; the issue is calibration enforcement and build errors.

## Data And Content Consistency

- [ ] `MonologuePool` declared count is stale.
  - `DramaticCount = 60`, but a direct count found 59 dramatic strings.
  - `GetTotalMonologueCount()` reports 80 while actual pool size is 79.

- [ ] Documentation/status is stale or contradictory.
  - `design/gdd/systems-index.md` marks many systems as not started while files exist.
  - `docs/notes/phase2-note-system.md` claims note system implementation status that does not match the wiring gaps found in code.

- [ ] `GAME_DESIGN.md` and code disagree on several implemented mechanics.
  - No duplicate roles.
  - Non-skippable calibration.
  - Silence room and wardrobe sound suppression.
  - Bell masking.
  - Signal jammer vs creak zones.
  - Playback trap.
  - Phase 3 radio/broadcast flow.

## Scene/Asset Checks Already Performed

- [ ] Recheck asset references after build fixes.
  - A scan of `AudioAssets` constants found no missing audio paths at the time of audit.
  - A scan of scene resource references did not print missing `res://` paths, but this should be repeated after scene changes.

## Attached Agent Progress Note

The attached pasted text described another agent's optimization-plan progress and noted that it could not finish because the build remained broken.

### Done And Committed By Other Agent

- [ ] T1 `FloorAssetCatalog` completed and committed as `150a1d4`.
  - Added static `Dictionary<string, PackedScene>` cache.
  - Added 14 KayKit asset keys.

- [ ] T2 `ListenerGroupCache` completed and committed as `414e525`.
  - Added autoload singleton holding cached `Godot.Collections.Array<ListenerAI>`.
  - Auto-refreshes via `NodeAdded` / `NodeRemoved`.

- [ ] T3 `PlayerController` performance work completed and committed as `92559fc`.
  - Separated debug-timer block from `FeedVoiceToObjectives`.
  - Added idle early-return when not holding radio and not speaking.

### Partial, Not Committed By Other Agent

- [ ] T4 `ListenerAI` skip-tick edits were made but not committed.
  - Added `_hasPendingEvent`, `_processTickAccumulator`, and `_processTickInterval`.
  - `HearNoise` sets the pending flag.
  - `_Process` has a skip-tick guard.
  - Blocked because the project build was broken.

- [ ] T13 build-error fix was started but not completed.
  - Reported start: 19 errors.
  - Reported reduced state: about 12 errors remaining.
  - Reported fixed items: missing `using` in `GameManager.cs` / `PlaybackManager.cs`, `UiSounds.cs` `SceneTree?.Root` syntax, `HUDManager.cs` switch expression, `GD.RandRange` casts in `PlayerController.cs` and `CreakZone.cs`, and some `FirstTimeSetup.cs` `AlignmentMode` changes.
  - Reported remaining blockers: `FirstTimeSetup.cs` CS0747 method calls in object initializers and `BoxContainer.AlignmentMode` enum mismatch.

### Not Started By Other Agent

- [ ] T5 reduce floor lights from 5 to 3 in four scenes.
- [ ] T6 create and run `Tools/convert_gltf_to_glb.py`.
- [ ] T7 wire Furniture Bits into F2/F3.
- [ ] T8 create and run `Tools/sweep_import_compress.py`.
- [ ] T9 async navmesh bake in `FloorNavigationBaker`.
- [ ] T10 create `FloorPropGrid` MultiMesh conversion and add it to four scenes.
- [ ] T11 create and run `Tools/measure_load_time.py`.
- [ ] T12 create and run `Tools/measure_vram.py`.

### Other Agent Summary

- [ ] 12 optimization tasks total: 3 done, 2 partial, 7 not started.
- [ ] Net committed progress reported as 25%.
- [ ] Remaining work was blocked behind Godot 4 API drift and build errors.
