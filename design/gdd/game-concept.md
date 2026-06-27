# Game Concept: Last Word

*Created: 2026-06-26*
*Status: Draft*

> This document is written in the Ashford Estate register: frightening systems,
> occasionally absurd content. Precision is not dry; it is the architecture of
> dread.

---

## Overview

*Last Word* is a 2-4 player online co-op horror survival game in which voice chat
is simultaneously your only tool and your greatest danger. Players are trapped
inside Ashford Estate, a decaying gothic manor haunted by The Listener — an
entity that tracks sound. Every time a player speaks in voice chat, they receive
the **Last Word Token**, a glowing skull mark visible through walls to all
players and to The Listener. The Listener hunts whichever living player holds
the Token. To escape, the group must find coded word notes hidden across the
estate, register them by reading them aloud, speak those words in the correct
sequence at the Clock Tower, and finally broadcast a 10-second monologue over
the radio — all while speaking draws the monster closer.

Silence is safety. Speech is power. The tension between those two facts is the
entire game.

Built in **Godot 4.x (.NET/C#)** for PC (Steam). Target session length: 25-35
minutes. Art style: low-poly dark gothic palette.

---

## Player Fantasy

The player fantasy of *Last Word* is: **"You are part of a desperate group
trapped in a haunted house where every word you speak could be your last."**

The player should feel:

- **Tension and guilt** — speaking to help a teammate puts your own life at risk
- **Betrayal and sacrifice** — the dynamics of the Token create moments where a
  teammate's panic dooms the group, or where one player volunteers to die for
the others
- **Emergent theatre** — every run produces a story worth retelling: the scream
that caused three more screams, the whispered warning that arrived too late,
the absurd Final Broadcast about a shopping list delivered while a monster
breaks down the door
- **Social trust under pressure** — coordinating strangers in random matchmaking,
or watching friends unravel as the estate darkens
- **Occasional absurdity** — the systems are frightening; the content is
  occasionally, deliberately ridiculous. A death card might quote your last
words in distorted pitch. The monologue pool includes both gothic fragments
and grocery lists.

This is the gothic horror-comedy register: the estate will kill you, and it
might make you laugh on the way out.

---

## Detailed Rules

### Core Loop

1. **Mic Calibration** — Before each run, every player completes a 30-second
   calibration. The game records their normal conversational RMS amplitude and
   sets personal thresholds for three voice tiers (whisper, normal, loud).
2. **Token Transfer** — Whenever a player's voice reaches Tier 1 (35%+ of their
   calibrated baseline) or above, the Last Word Token transfers to them
   instantly. The Token is visible through walls to all players and The
   Listener.
3. **Listener Hunt** — The Listener navigates toward the Token holder. Its
   behaviour depends on its AI state: Idle, Alerted, Hunting, or Frenzy.
4. **Three Phases of Escape:**
   - **Phase 1: Find the Words (0-15 min)** — Locate four coded word notes on
     floors 2 and 3. Pick up a note (silent `E` hold), then read the word aloud
     into voice chat to register it. The speaking player receives the Token.
     The Mute role may carry notes to Registration Boards for teammates to
     read; the Archivist role (post-launch) may register silently by holding
     the note for 5 seconds.
   - **Phase 2: Speak the Sequence (15-25 min)** — Surviving players converge
     at the Clock Tower and speak the registered words in the correct
     sequence. Correct order is randomized per run and only revealed after all
     required words are registered. Each word requires sustained Tier 2
     amplitude (100%+ baseline) for at least 0.5 seconds. All designated
     speakers must remain within 10 metres of the clock mechanism. Wrong order
     resets the sequence and locks the mechanism for 30 seconds. If only one
     player survives, the single-survivor fail state triggers with a 10-second
     grace period.
   - **Phase 3: The Final Broadcast (post-25 min)** — After Phase 2 completes,
     one player picks up the radio and speaks a 10-second monologue. The
     Listener enters **permanent Frenzy** for the entire phase. If the
     broadcaster stops speaking for more than 2 seconds, the timer freezes and
     the Listener re-locks. Any surviving player may pick up the radio if the
     broadcaster dies; the timer resets to 0 on each pickup. Victory: complete
     the full 10-second monologue.
5. **Death & Spectator** — If The Listener catches a player, they die after a
   0.5-second grab animation and a distorted playback of their last voice line.
   Dead players become Whispers — translucent spectators who may place
   temporary skull map markers (`J`) visible to all living players for 15
   seconds each.
6. **Escalation Timeline** — The manor grows more hostile on a fixed clock:
   - **10 min:** Alerted-state silence reset suspended; Listener hunts within
     15 seconds of being alerted.
   - **20 min:** A second Listener spawns in the Basement, targeting the player
     with the highest cumulative speaking time (independent of Token).
   - **25 min:** Lights Out permanently. Torches (90s battery) become
     essential. The Clap (`Q`) unlocks for echolocation at a Tier 0 sound cost.
   - **30 min:** A 3-minute countdown begins. If the broadcast is not
     completed, a third Listener spawns and the radio breaks.

### Roles (MVP)

- **The Loud** — Can emit a 5-second stun pulse (`F`, 90s cooldown).
- **The Static** — Can deploy a white-noise bubble (`R`, 2 charges per run).
- **The Mute** — Cannot speak above Tier 1 (whisper) under any condition. Has
  access to a radial gesture wheel with 5 additional silent communication
  options. Can carry notes to Registration Boards for silent handoff.
- **The Echo** — Post-launch. Can replay teammate voice clips as decoys (`T`).

---

## Formulas

| System | Formula / Rule |
|--------|----------------|
| **Tier 1 (Whisper) threshold** | 35% of calibrated baseline amplitude |
| **Tier 2 (Normal) threshold** | 100% of calibrated baseline amplitude |
| **Tier 3 (Loud/Scream) threshold** | 200% of calibrated baseline amplitude |
| **Token transfer** | Triggers on any audio event meeting or exceeding Tier 1 (35%+ baseline) |
| **Tier hysteresis buffer** | 5% at each boundary (e.g., drops from Tier 1 to Tier 0 only below 30%) |
| **Listener idle speed** | 0.3x base human sprint speed |
| **Listener hunting speed** | 1.0x base human sprint speed (1.3x multiplier applied, exact m/s depends on player base) |
| **Listener scream Frenzy speed** | 1.8x for 12 seconds |
| **Listener attack range** | 5 metres |
| **Tier 0 detection radius** | 4 metres (sub-whisper / environmental) |
| **Tier 1 detection radius** | 8 metres |
| **Tier 2 detection radius** | 20 metres |
| **Tier 3 detection radius** | Entire floor (unlimited) |
| **Vocal Sacrifice lock duration** | 30 seconds |
| **The Loud stun duration** | 5 seconds (90s cooldown) |
| **Playback trap trigger** | 90 consecutive seconds of full group silence (post-launch; 60s/45s under Metric B escalation) |
| **Phase 2 sequence lockout** | 30 seconds on wrong order |
| **Phase 3 broadcast timer** | 10 seconds (resets to 0 on radio handoff) |
| **Phase 3 silence freeze** | Broadcaster stops speaking for >2 seconds |
| **Vocal imprint profile decay** | 60 seconds to zero after player death |
| **Lights Out torch battery** | 90 seconds per battery |
| **Clap cooldown** | 12 seconds |
| **Spectator skull marker duration** | 15 seconds |
| **Single-survivor grace period** | 10 seconds |
| **2-player Phase 3 Frenzy speed reduction** | -15% sprint speed |
| **Text Broadcaster monologue speed** | 0.6x (10s monologue takes ~16.7s to type) |
| **Undistorted Playback availability** | Disabled first 10 minutes in random matchmaking; 30% rate at Strong profile tier thereafter |

---

## Edge Cases

| Edge Case | Resolution |
|-----------|------------|
| **Token-less state at run start** | No Token exists until first speech. Listener patrols in Idle state. First Tier 1+ speech creates Token and target simultaneously. |
| **Token-less state mid-run** | If the Token holder dies and no one else has spoken, the Listener navigates toward the most recently detected sound source. In Phase 3, the Listener targets the radio's physical location. |
| **Death mid-registration (Phase 1)** | Phase 1 word registration is instantaneous on valid speech detection. If speech is detected before the grab animation initiates, the word registers; the grab does not retroactively cancel a speech event already processed. |
| **Single-survivor fail state (Phase 2)** | Triggers when surviving player count drops to 1, regardless of session size. A 10-second grace period counts down. If the last player dies during grace, run ends with full-party-wipe. |
| **Wrong-order sequence (Phase 2)** | Sequence resets. Clock mechanism locks for 30 seconds. Players must wait, evade, and retry. |
| **Mute forced Tier 1** | The Mute role cannot exceed Tier 1 amplitude regardless of input volume. This is enforced server-side. If a Mute speaks at Tier 1+, they accumulate speaking time and can be targeted by the second Listener. |
| **Loud always Tier 2+** | The Loud role cannot speak below Tier 2 (normal speech threshold). Whispering is mechanically impossible for this role. |
| **Text Broadcaster does not count as voice** | Text Broadcaster mode synthesizes audio for the Listener's detection but does not count as a "voice" for Phase 2 word-speaking purposes. If the only speaking-capable player is a Text Broadcaster and their partner dies in Phase 2, the single-survivor fail state triggers immediately. |
| **Radio pickup vs. grab race condition** | Radio pickup requires a 1-second `E` hold. Listener grab animation completes in 0.5 seconds. A player beginning pickup while the Listener is already within grab range will always lose the race — intentional. |
| **ESC menu during broadcast** | Opening ESC during Phase 3 does not pause the game. The 2-second silence penalty applies immediately; an overlay warns the broadcaster. |
| **Vocal Sacrifice + The Loud stun simultaneous** | Stun applies first (5s), then Sacrifice lock (30s). The 30-second countdown pauses during stun. The Loud's 90s cooldown begins at `F` press, not when the stun ends. |
| **Session abandonment near 20-minute Duplication** | Abandoning between minute 20:00 and 20:45 incurs a 5-minute cooldown to prevent exploit. |

---

## Dependencies

The *Last Word* game concept depends on the following systems:

1. **Voice Detection & Tier Classification** — Real-time microphone RMS amplitude
   measurement, per-player calibration, tier classification with hysteresis
   buffers, and integration with Godot Voice / WebRTC.
2. **Last Word Token System** — Instantaneous Token transfer on Tier 1+ events,
   server-authoritative tracking, wall-visible glow, HUD display with holder
   name and duration.
3. **The Listener AI State Machine** — Idle, Alerted, Hunting, Frenzy states
   with distinct speeds, detection radii by tier, imprint-profile-based
   secondary targeting, and adaptive Metric A/B escalation (post-launch).
4. **Multiplayer Networking** — Client-server architecture with host authority,
   voice-tier synchronization across clients, random matchmaking lobby system,
   and player state replication (alive / dead / spectator).
5. **Objective Phase System** — Three-phase progression (Find Words, Speak
   Sequence, Final Broadcast) with randomized note placement, word pool
   selection, sequence generation, and victory/fail-state resolution.
6. **Gesture & Silent Communication** — 8 standard keybind gestures plus radial
   wheel, no sound signature, no Token transfer.
7. **Escalation Timeline Engine** — Fixed-timer events (10/20/25/30 min marks)
   that modify Listener parameters, spawn additional entities, and alter level
   lighting globally.

---

## Tuning Knobs

| Knob | Default | Description |
|------|---------|-------------|
| `tier1_threshold` | 35% | Whisper entry point (% of calibrated baseline) |
| `tier3_threshold` | 200% | Loud/scream entry point |
| `hysteresis_buffer` | 5% | Dead zone below each tier boundary to prevent flicker |
| `listener_idle_speed` | 0.3x | Movement multiplier in Idle state |
| `listener_hunting_speed` | 1.0x | Movement multiplier in Hunting state |
| `listener_frenzy_speed` | 1.8x | Sprint multiplier during Scream Frenzy |
| `listener_attack_range` | 5m | Grab/trigger distance |
| `tier0_detection_radius` | 4m | Sub-whisper / environmental sound range |
| `tier1_detection_radius` | 8m | Whisper detection range |
| `tier2_detection_radius` | 20m | Normal speech detection range |
| `scream_frenzy_duration` | 12s | How long Tier 3 sprint lasts |
| `vocal_sacrifice_duration` | 30s | Lock duration |
| `loud_stun_duration` | 5s | Stun freeze time |
| `loud_stun_cooldown` | 90s | Time between stun uses |
| `static_charge_count` | 2 | White-noise bubble uses per run |
| `playback_silence_threshold` | 90s | Seconds of group silence before Playback Trap |
| `phase2_lockout_duration` | 30s | Wrong-order penalty |
| `phase3_broadcast_duration` | 10s | Target monologue length |
| `phase3_silence_tolerance` | 2s | Allowed gap before timer freezes |
| `torch_battery_duration` | 90s | Seconds of light per battery |
| `clap_cooldown` | 12s | Echolocation ability cooldown |
| `imprint_decay_time` | 60s | Seconds for dead player's profile to decay |
| `single_survivor_grace` | 10s | Phase 2 fail-state countdown |
| `phase3_2player_speed_reduction` | 15% | Sprint reduction for 2-player sessions |
| `text_broadcaster_speed` | 0.6x | Typing-time multiplier for accessibility mode |
| `undistorted_playback_delay` | 10 min | Matchmaking delay before undistorted Playback |
| `undistorted_playback_rate` | 30% | Chance of undistorted trap at Strong imprint |
| `final_deadline_countdown` | 3 min | Time before third Listener spawns at 30 min |
| `escalation_marks` | [10,20,25,30] | Minutes at which escalation events fire |

---

## Acceptance Criteria

- [ ] A new player can read this document and explain the core loop in one
      sentence: "Speak to progress, but every word makes the monster hunt you."
- [ ] All seven design pillars from GAME_DESIGN.md are represented in at least
      one section of this document.
- [ ] All formulas in the Formulas section match the values specified in
      GAME_DESIGN.md sections 3.1-3.3 and 4.2.
- [ ] Every edge case listed here has a corresponding, unambiguous resolution
      rule.
- [ ] The document uses a consistent gothic horror-comedy tone — frightening
      systems, occasionally absurd content — and never slips into dry corporate
      register.
- [ ] The Ashford Estate is named as the setting; the Last Word Token, The
      Listener, and the Three Phases are described as the central systems.
- [ ] No new mechanics are invented that do not appear in GAME_DESIGN.md.
- [ ] Roles, abilities, keys, and cooldowns match the authoritative controls
      reference in GAME_DESIGN.md section 3.7.
- [ ] Tuning Knobs section contains at least one configurable parameter for
      every mechanical system described in Detailed Rules.
- [ ] Dependencies section lists every high-level system the concept requires,
      in priority order.

---

*End of Game Concept — Last Word*
