# Last Word — Quick Section Checklist

Updated after Voice Mechanics MVP implementation and C# build verification
(`dotnet build -LastWord.csproj` succeeded, 0 errors, 0 warnings).

| § | Section | Done | Partial | Not started | Notes |
|---|---|---:|---:|---:|---|
| 3 | Voice Mechanics | 12 | 0 | 0 | Token/tier/calibration/gestures/radial/clap/sacrifice/Loud/Static + Imprinting + Playback Trap done |
| 4 | The Listener AI | 7 | 0 | 0 | State machine + attack ranges + BT + escalation + death sequence + mimicry + Adaptive Evolution done |
| 5 | Player Roles | 12 | 0 | 0 | Role selection + No/Loud/Static/Mute/Archivist/Witness + passives + abilities + death notification done |
| 6 | Map Design — Ashford Estate | 14 | 0 | 0 | F1–F4 scenes + all interactive props (intercom/wardrobe/phones/Silence Room/barricade/bell/radio) done |
| 7 | Objective System | 12 | 0 | 0 | SequenceManager + NoteItem spawn + broadcast/wire + win/HUD markers done |
| 8 | Escalation Timeline | 4 | 0 | 0 | EscalationTimer.cs with 10/20/25/30 hooks done |
| 9 | Death & Spectator | 7 | 0 | 0 | FadeToBlack, DeathCard, WhisperMode, Listener repulsion, J-marker, party wipe all done |
| 10 | Items & Environmental | 0 | 0 | 11 | Lighter/torch/gramophone/barricades/knockover all missing |
| 11 | UI & HUD | 0 | 4 | 13 | Token/voice meter partial; phase tracker/teammate icons/accessibility missing |
| 12 | Audio Design | 0 | 1 | 8 | Idle hum wired; all other SFX assets + integrations missing |
| 13 | Multiplayer | 1 | 4 | 5 | `NetworkManager` + token latency done; disconnect/mic watchdog/vote-kick/VC transport missing |
| 14 | Progression & Replayability | 0 | 1 | 7 | Save schema partial; stats/achievements not started |
| 15 | Achievements | 8 | 0 | 0 | `AchievementManager` autoload (`Scripts/Core/AchievementManager.cs`) + 8 funny ones wired: 👂 It Sees You · 🤐 Mime Time · 📻 Wrong Number · 🫁 Last Breath · 🎭 Method Actor · 🏃 Marathon of Silence · 💀 Final Girl · 🏆 The Last Word |
| 18 | Random Matchmaking | 0 | 2 | 5 | Lobby scene exists but no browser/list/region; privacy banner missing |

**Total:** 65 done · 14 partial · 57 not started · 1 blocked (GameScene crash) · 5 deferred (Steam)

---

## Last-week MVP targets (P0 / vertical slice)

1. **§7.1 Phase 1 wiring** — NoteItem collider + F1_Basement spawn markers + RegistrationBoard placement
2. **§8.1 EscalationTimer** — minute-10/20/25/30 hooks
3. **§9.1 Death fade + death card** — completes the death loop
4. **§11.4 First-Time Setup screen** — required before new players can calibrate
5. **§5 Role selection UI** — unlocks Loud/Mute/Static in lobby
6. **§7.2 SequenceManager completion** — wrong-order reset, 30s lock, scaling
7. **§6.1 F1 Basement interactive setup** — navmesh, Listener spawn, NoteSpawn markers

For the full detailed matrix see `CHECKLIST.md`.

