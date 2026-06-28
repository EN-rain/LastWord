# Last Word - Quick Checklist

Updated after the 2026-06-27 repair pass.

## Verification

- [x] `dotnet build -v:minimal` passes with 0 warnings and 0 errors.
- [x] `dotnet test -v:minimal` completes; 8 Godot-runtime tests are intentionally skipped.
- [x] Godot MCP project startup/stop works with `finalErrors: []`.
- [x] Asset reference sweep reports `missing_count=0`.

## Gameplay And Systems

- [x] Objective correctness gaps fixed: Phase 1 spoken-word verification, Phase 2 speech validation, empty sequence completion, and carried-radio Phase 3 targeting.
- [x] Role/ability gaps fixed: duplicate-role guards, Mute passive handling, Vocal Sacrifice subscription, Clap tuning, Static visibility, Echo listener baiting, and Witness burst.
- [x] Audio/system gaps fixed: playback trap reporting, `VoiceRecorder` cleanup, configurable dev voice keys, achievements, adaptive evolution, and imprint death profiles.
- [x] Scene/content/doc drift fixed or documented: RegistrationBoard radius, DeadPhone hints, escalation/second Listener semantics, settings input drift, monologue count, asset references, and GDD status note.

## Optimization

- [x] Floor lights reduced to 3 per floor scene.
- [x] Furniture Bits converted to 53 GLBs and wired into F2/F3.
- [x] Import compression sweep created and run; latest run checked 500 files and modified 0 after the prior 118-file update.
- [x] `FloorPropGrid` MultiMesh batching added to all four floor scenes.
- [x] Load/VRAM measurement scripts created; load script reports missing Godot executable when `godot` is not on PATH.

## Current Counts

- Detailed audit items fixed: 98
- Detailed audit items still open: 0

`ISSUES_TO_FIX.md` was deleted because all tracked issues were fixed or explicitly converted into verified follow-up notes.
