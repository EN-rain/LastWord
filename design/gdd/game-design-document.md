<!--
Last Word — Game Design Document
Derived from GAME_DESIGN.md (v2.5)
CCGS Template: game-design-document.md
-->

# Last Word — Game Design Document
### Version 2.5 Engine: Godot 4.x (.NET/C#)

## Overview

*Last Word* is a four-player cooperative horror game built in Godot 4.6 + C# where voice chat is the central mechanic. Players explore the Ashford Estate, a gothic manor whose silent resident — the Listener — hunts whoever spoke last. The core tension comes from the fact that every useful action requires speech, yet every syllable draws the enemy closer. The game is frightening in its systems and occasionally absurd in its content, producing emergent stories of guilt, betrayal, and heroic sacrifice.

## Player Fantasy

The player should feel the dread of being the last one who spoke, the relief of passing that danger to a teammate, and the dark comedy of watching a panicked friend accidentally doom everyone by shouting. The fantasy is *shared vulnerability through communication*: your voice is both your only tool and your greatest liability.

## Core Loop

1. **Calibrate** — each player captures a 30-second vocal baseline.
2. **Explore** — players search floors 2 and 3 of Ashford Estate for four coded-word notes while avoiding the Listener.
3. **Register** — a player picks up a note, reads the word aloud (Tier 1+), and receives the Last Word Token.
4. **Sequence** — at the Clock Tower, surviving players speak the registered words in a randomized order, sustained at Tier 2 for 0.5s each. Wrong order resets the sequence and locks the mechanism for 30 seconds.
5. **Broadcast** — one player speaks a 10-second monologue into the radio at sustained Tier 2 while the Listener enters permanent Frenzy.
6. **Resolve** — complete the broadcast to escape, or die and join the Whisper spectators.

## Table of Contents

1. [Game Overview](#1-game-overview)
2. [Core Design Philosophy](#2-core-design-philosophy)
3. [Voice Mechanics â€” The Heart of the Game](#3-voice-mechanics--the-heart-of-the-game)
4. [The Listener â€” Enemy Design](#4-the-listener--enemy-design)
5. [Player Roles](#5-player-roles)
6. [Map Design â€” Ashford Estate](#6-map-design--ashford-estate)
7. [Objective System â€” Three Phases](#7-objective-system--three-phases)
8. [Escalation & Pacing](#8-escalation--pacing)
9. [Death & Spectator System](#9-death--spectator-system)
10. [Items & Environmental Interactions](#10-items--environmental-interactions)
11. [UI & HUD Design](#11-ui--hud-design)
12. [Audio Design](#12-audio-design)
13. [Multiplayer Architecture](#13-multiplayer-architecture)
14. [Progression & Replayability](#14-progression--replayability)
15. [Steam Achievements & Viral Hooks](#15-steam-achievements--viral-hooks)
16. [Solo Dev Scope & Priorities](#16-solo-dev-scope--priorities)
17. [Business Model & Pricing](#17-business-model--pricing)
18. [Random Matchmaking System](#18-random-matchmaking-system)

---

## 1. Game Overview

**Title:** Last Word
**Genre:** Multiplayer Co-op Horror Survival
**Players:** 1â€“4 online, voice chat required
**Platform:** PC (Steam)
**Engine:** Godot 4.x (.NET/C#)
**Target Session Length:** 25â€“35 minutes per run
**Art Style:** Low poly with a consistent dark gothic palette (see Â§16.3 for asset guidance)
**Tone:** Horror-comedy â€” frightening mechanics delivered with moments of absurdity. This is explicit and consistent across all design decisions.

### One-Line Pitch
> A **2â€“4 player** horror game where the monster hunts whoever spoke last in voice chat â€” speaking is your only tool and your greatest danger.

### High Concept
Four players are trapped inside Ashford Estate, hunted by The Listener â€” an entity that tracks sound. To escape, players must read coded words aloud, coordinate verbally, and complete a final 10-second radio broadcast. Every time a player speaks in voice chat, they become the monster's next target. The player who spoke most recently carries the **Last Word Token** â€” a glowing mark visible through walls to everyone, including the monster.

Silence is safety. Speech is power. The tension between those two facts is the entire game.

> **Note on external voice chat:** The Token only tracks in-game VC. Players who route communication through Discord or phone calls bypass the risk system entirely â€” and know it. This cannot be patched. It is a social contract. The store page, onboarding, and lobby screen all make this explicit: playing with external VC removes the entire point of the game, not just a rule. The in-game VC is the experience. Design all coordination objectives to require in-game voice-triggered events (not just information exchange) so that in-game VC is strictly more useful than external tools wherever possible.

### What Makes It Unique
- Voice chat is not just a communication tool â€” it is a **gameplay mechanic with consequences**
- The monster **adapts in real time** to how your group uses voice chat
- Every objective **requires speech** to complete â€” you cannot stay silent and win
- No game currently on Steam uses the act of speaking as a primary risk mechanic
- **Public random matchmaking** supports solo players who want to find strangers without a pre-formed group, while private custom rooms may also start solo for testing, practice, and challenge runs (see Â§18)

---

## 2. Core Design Philosophy

### Pillar 1: Every Action Has a Voice Cost
No meaningful action in the game is free from the voice mechanic. Reading a note costs you the Last Word Token. Asking a teammate for help costs you the Last Word Token. Screaming in panic costs you the Last Word Token and triggers a sprint. Players must constantly evaluate whether communication is worth the risk.

### Pillar 2: Emergent Stories, Not Scripted Ones
The game does not tell players what to do moment-to-moment. The systems create situations â€” a player holding the Token near the Listener, a teammate deciding whether to warn them and take the Token themselves, a dead player watching helplessly â€” and the players create the story. Every run should produce a moment worth retelling.

### Pillar 3: Asymmetric Knowledge Creates Tension
The Listener knows who spoke last. Living players know their own location. Dead players know the Listener's location but can barely communicate it. No single player has full information. The gap between what players know and what they need to know is where horror lives.

### Pillar 4: Failure Is Entertaining
Dying should be funny or horrifying, never frustrating. The death animation replays the dead player's own last voice line in a distorted pitch. Losing a run because someone screamed at a spider is a story, not a failure. Short sessions (30 min) mean restarts feel light.

### Pillar 5: Streamability Is a Feature
Every core system is designed to produce clippable moments. The Token passing mechanic. The Listener mimicking a player's voice. The Final Broadcast monologue. These are not accidents â€” they are intentional viral hooks baked into gameplay.

### Pillar 6: Horror-Comedy Is a Commitment, Not an Accident
*Last Word* is a horror-comedy. It is frightening in its systems and occasionally absurd in its content. This tone must be consistent. The Final Broadcast pool, the death card texts, the Listener's behaviours, the store page copy, and the audio design all lean into this dual register together. Do not design horror in eleven sections and comedy in one.

### Pillar 7: Accessible to Solo Queue
The random matchmaking system (Â§18) is not an afterthought â€” it is a player acquisition path. A player who cannot convince three friends to buy the game simultaneously should still be able to experience the core loop with strangers. The social and communication design must account for groups of unknown players with no pre-existing trust or rapport.

---

## 3. Voice Mechanics â€” The Heart of the Game

### 3.1 The Last Word Token âš‘

The Last Word Token is a glowing skull icon that floats above whichever player spoke most recently in voice chat. It has the following properties:

- **Visible through walls** to all players and to The Listener
- **Transferred instantly** when any other player speaks
- **Tracked by The Listener** as its primary navigation target
- **Displayed on HUD** showing current Token holder's name and how long they have held it
- **Cannot be dropped** â€” only transferred through speech

**Initial state at run start (v1.3):** No Token exists until the first player speaks. At run start, the Listener begins in pure Idle/Patrol state with no primary target. The first player to speak in any tier immediately becomes the Token holder and the Listener's target. This creates a deliberate opening pressure: players must decide who absorbs the first-speech risk before splitting to find notes. Communicate this in the orientation mode and the lobby pre-run tip.

**Token transfer trigger definition:** Token transfer is triggered by any audio event that meets or exceeds the Tier 1 threshold (35%+ of calibrated baseline) on that player's microphone input, regardless of whether the audio is intentional speech. Background noise, coughing, or ambient sounds that exceed 35% baseline will transfer the Token. Players are responsible for managing their microphone environment. A noise gate (minimum amplitude filter below 20% baseline) suppresses environmental noise below the Tier 0 floor, but sounds between 20â€“34% that briefly spike above 35% will still trigger transfer. Recommend all players use a close-microphone setup and manage ambient noise before sessions.

**No-Token Listener behaviour (v1.4):** Whenever the Token does not exist â€” at run start before first speech, or in Phase 3 if the broadcaster dies mid-broadcast before anyone else has spoken â€” the Listener navigates toward the most recently detected sound source and holds Alerted state. During Phase 3 specifically, if no Token exists, the Listener treats the radio's physical location as its primary navigation target (the broadcast is an ongoing sound event regardless of who holds the radio). This rule closes the gap where Frenzy state would otherwise have no defined target.

The Token creates a natural hot-potato dynamic without any explicit game instruction. Players learn within the first 30 seconds that speaking transfers their safety to someone else. This creates guilt, strategy, and betrayal all at once.

### 3.2 Mic Calibration

Before voice tier detection goes live in any session, every player completes a mandatory 30-second mic calibration step.

**Calibration flow:**
1. Lobby screen displays a mic level meter for each player before ready-up
2. Each player speaks a short phrase at their normal conversational volume ("I'm ready")
3. The game records their RMS amplitude average over 3 seconds and sets that as their **personal baseline** (this is the Tier 2 entry point â€” normal speech)
4. Tier 1 (whisper) threshold = 35% of their baseline
5. Tier 3 (loud/scream) threshold = 200% of their baseline
6. Players with AGC-enabled hardware are warned and prompted to disable AGC in Windows Sound settings, with a link to instructions

This calibration persists per device and can be re-run from the settings menu at any time. It is non-skippable for first-time players and skippable-with-warning for returning players.

**Random matchmaking note:** In public lobbies, players may join with calibration already complete from a previous session. The lobby mic meter always displays live levels regardless, so hosts and teammates can visually confirm a new player's mic is functioning before the run begins.

### 3.3 Voice Detection Tiers

The game uses Godot's Godot Voice/WebRTC (see Â§13 for SDK risk mitigation) to measure real-time microphone volume and classify speech into three tiers. All thresholds are relative to the calibrated baseline.

#### Tier 0: Sub-Whisper / Environmental (Below 35% of Baseline)

This tier covers all sounds below the whisper threshold â€” including environmental interactions (the Clap at `Q`, furniture knockovers, creaking floors) and any player audio that falls beneath Tier 1. The Listener responds to Tier 0 sounds but with a reduced detection radius:

- Listener attraction radius: **4 metres**
- Listener enters Alerted state on detection, not Hunting
- Eye sockets show **no change** from Idle â€” the Listener reacts without visibly signalling it has heard something
- Does **not** transfer the Last Word Token
- Does **not** contribute to vocal imprinting

**Design note:** Tier 0 gives environmental sounds a consistent mechanical home. The Clap (Â§3.7) sits here at 15% baseline â€” dangerous if the Listener is within 4 metres, safe otherwise. Furniture knockovers at 30% baseline also sit here and follow the same 4-metre rule. This replaces the ambiguous "draws an Alerted Listener" language that previously appeared for each individual interaction.

#### Tier 1: Whisper (35%â€“99% of Baseline)
- Listener attraction radius: **8 metres**
- Teammates hear you at **60% volume** (requires concentration)
- Puzzle voice triggers activate but take **3Ã— longer** to register
- Listener's eye sockets glow **faintly blue** â€” it senses something but cannot locate you
- Last Word Token transfers normally

**Tier 1 AI behaviour (v1.5):** Tier 1 detection does not trigger a named AI state transition. The Listener remains in its current state (Idle or Alerted) but gains a **visual cue** (faintly blue eye glow) and an **orientation behaviour** â€” it turns its head toward the sound source but does not move or pathfind. The blue glow persists for 3 seconds after the last Tier 1 sound. This is distinct from the Alerted state (amber glow, begins pathfinding). Tier 1 tells the Listener "something is over there" without confirming a target.

**Tier 1 distinction â€” player voice vs. environmental source:**
- **Tier 1 â€” Player voice:** Listener turns toward sound source, no pathfinding. Eye sockets glow faintly blue. The Listener cannot locate a moving player from Tier 1 voice alone.
- **Tier 1 â€” Environmental source (fixed position, e.g. Gramophone):** Listener enters Alerted state and pathfinds to the source's position. A fixed sound source provides confirmed location data; a moving player's voice does not.

**Design note:** Whispering is a skill. It requires players to genuinely lower their real-world voice, which causes physical tension that maps perfectly to the horror tone. Players who learn to whisper effectively gain a meaningful advantage. Speaking below your calibrated baseline (35%â€“99%) is the whisper tier.

#### Tier 2: Normal Speech (100%â€“199% of Baseline)
- Listener attraction radius: **20 metres**
- Teammates hear at **full volume**
- Puzzle voice triggers activate at normal speed
- Listener's eye sockets glow **amber** and it begins pathfinding toward last known position
- Last Word Token transfers immediately

> **Design note:** The calibrated baseline (100%) is the entry point for Tier 2. Speaking at your natural calibrated volume is a Tier 2 event by design â€” normal speech carries moderate risk. Players who want to stay in Tier 1 must consciously speak below their baseline. This is intentional: the calibration point represents "normal risk," not "safe."

> **Hysteresis buffer (v1.5):** To prevent tier flickering at boundaries, tier transitions use a 5% hysteresis buffer. Tier 1 triggers at 35% baseline, but does not drop to Tier 0 until amplitude falls below 30% baseline. Tier 2 triggers at 100% baseline, but the player does not drop back to Tier 1 until amplitude falls below 95% baseline. Similarly, Tier 3 triggers at 200% baseline, but the player does not drop to Tier 2 until amplitude falls below 190% baseline. This prevents rapid tier oscillation and unstable Token transfers when a player's voice hovers near a threshold. **Dead zone (30â€“34%):** Players whose amplitude sits between 30â€“34% of baseline (above the Tier 0 floor but below the Tier 1 entry threshold) remain in Tier 0. This zone produces no Token transfer and only 4-metre detection â€” it is functionally sub-whisper. Players must exceed 35% to enter Tier 1. This dead zone is intentional: it gives players a narrow safety margin around the whisper threshold without requiring perfect amplitude control.

#### Tier 3: Loud / Scream (Above 200% of Baseline)
- Listener attraction radius: **entire floor** (unlimited)
- All players hear a sharp audio distortion on their end â€” involuntary jump-scare feedback
- Listener **sprints at 1.8Ã— speed** for 12 seconds toward source
- Nearby physics objects shatter (glasses, picture frames)
- Other players' screens flash white static briefly
- Token transfers normally, but the scream response follows the Listener target priority rules in Â§4.2.1. A detected scream locks the Listener onto the screamer for the 12-second Scream Frenzy window.

**Design note:** The screen flash for OTHER players is critical. It means a teammate screaming causes everyone to panic simultaneously, which causes more screaming, which is exactly what happens in every great Lethal Company clip.

### 3.4 Voice Imprinting System

The Listener builds a persistent vocal profile for each **living** player throughout the run. This is not a binary "heard/not heard" system â€” it accumulates.

| Cumulative Speaking Time | Listener Behaviour |
|---|---|
| 0â€“5 seconds | No profile. Listener only tracks Last Word Token. |
| 5â€“15 seconds | Weak profile. Listener occasionally glances toward player even if not Token holder. |
| 15â€“30 seconds | Moderate profile. Listener checks player's last known location after clearing Token holder. |
| 30+ seconds | Strong profile. Listener tracks this player simultaneously alongside Token holder, alternating attention. |

Profiles **do not reset between floors**. A player who spoke frequently early game carries that risk for the entire run. This rewards discipline and punishes chattiness in a compounding way. **Cooperative incentive:** Groups who distribute note-reading across all players (each reading one note) accumulate lower individual imprint profiles, delaying second Listener lock-on at the 20-minute mark. This is an intended cooperative incentive â€” the imprint system rewards task distribution over individual efficiency. Communicate this dynamic in orientation mode or via a death card tip.

**Profile decay on death (v1.4):** When a player dies, their vocal imprint profile decays to zero over **60 seconds**. The Listener does not immediately abandon interest â€” a Strong-profile player who just died will still draw brief attention for up to a minute â€” but the profile cannot grow further and will not persist. After 60 seconds, the dead player's profile is fully cleared and the Listener no longer references it. This prevents a dead player's accumulated profile from indefinitely affecting the AI, and prevents griefing scenarios where a dead Echo's profile continues to misdirect the Listener through the rest of the run.

**Phase 3 Frenzy note:** During Phase 3 Frenzy, the Listener's secondary imprint-based targeting is suspended â€” Frenzy overrides all profile behaviour. Imprint decay still runs during Phase 3 but has no observable effect while Frenzy is active.

### 3.5 Playback Trap

After any period of **90 consecutive seconds of full group silence**, the Listener activates its Playback system:

1. It selects the most frequently-spoken living player's voice profile (see Â§4.4 for clip selection)
2. It replays a 3â€“5 second snippet of that player's actual recorded speech from earlier in the run, played back through a distortion filter
3. The playback originates from a random room the Listener currently occupies
4. If any player verbally responds to the playback, their position is confirmed and the Listener sprints

**Trap variation â€” the undistorted playback:** Once the Listener's voice imprinting profile for any living player reaches the 30+ second tier (Strong), it gains access to a second Playback mode. In this mode, the recorded snippet plays with *no distortion* â€” it sounds identical to the real player's voice. The Listener uses this mode in 30% of Playback activations at Strong profile tier.

**Random matchmaking undistorted Playback delay:** In random matchmaking sessions (created via public lobby browser, Â§18.2), the undistorted Playback mode is disabled for the first 10 minutes of the run â€” only distorted Playback activates until players have had time to learn each other's voices. After minute 10, undistorted Playback becomes available at the standard 30% rate. This applies only to public lobby sessions â€” private code sessions always use the standard undistorted rate from run start.

**Silence threshold escalation:** The 90-second trigger is the default. If the Adaptive System (Â§4.3) has elevated Metric B (silence discipline), the trigger drops to 60 seconds. At maximum Metric B elevation, Playback can trigger after 45 seconds of silence.

> **Note (v1.4):** Both the base Playback trigger (90 seconds) and its Metric B acceleration are post-launch features and activate together. Neither is present in Early Access.

**Random matchmaking note:** The Playback trap is most effective when players recognise each other's voices. In random lobbies, players may not yet have learned their teammates' vocal signatures. This makes undistorted Playback even more dangerous early in a session, as players lack the familiarity to doubt what they hear.

> **Scope note:** Playback trap is a **post-launch feature** (see Â§16.1). It is documented here for design intent. All Playback-related HUD elements and items are inactive in Early Access. The Adaptive system (including Metric B acceleration of the Playback trigger) is also post-launch â€” the 90-second base trigger is the only active value in Early Access.

### 3.6 Vocal Sacrifice âš‘

Any player can intentionally activate Vocal Sacrifice by holding the `G` key for 1 second, then speaking loudly. The `G` key is reserved exclusively for Vocal Sacrifice across all roles â€” no role ability shares this key.

The 1-second hold triggers a **visible pre-signal**: the activating player's character flashes a brief amber pulse **estate-wide** â€” it appears on the HUD teammate status bar (the four icons at screen edge) as a brief flash of the sacrificing player's icon turning amber for 1 second, visible to all players regardless of distance. This gives teammates a 1-second warning before the sacrifice lock activates.

**Activation speech classification:** The activation speech for Vocal Sacrifice bypasses Tier 3 classification regardless of amplitude. It registers as a **special Tier 2.5 event** â€” sufficient to transfer the Token and trigger the lock, but it does not cause Tier 3 side effects (no screen flash, no physics shatter, no Frenzy sprint). The lock itself is the consequence; the activating player is not punished twice. **Imprinting:** The Vocal Sacrifice activation speech counts as Tier 2 for imprinting purposes â€” it adds to the activating player's cumulative speaking time (Â§3.4).

Once triggered:
- Immediately locks the Listener's target onto the sacrificing player for **30 seconds**, regardless of who holds the Token
- Causes the Listener to abandon any current hunt and redirect
- **Vocal Sacrifice lock overrides Token targeting, Hunting, Alerted, and non-Frenzy sprinting. It does not interrupt Phase 3 Permanent Frenzy or an active Scream Frenzy lock.**
- Gives all other players a **safe window** to speak normally, solve puzzles, or move loudly without changing the Listener's lock target. This does not protect players who enter the 5-metre attack range while moving or making sound.
- The sacrificing player must survive alone for those 30 seconds with no VC help from teammates
- After 30 seconds, the lock expires and normal Token tracking resumes
- A countdown timer (30â†’0) appears on all players' HUDs during the sacrifice window

**Token behaviour during Vocal Sacrifice lock (v1.3):** The Token still transfers normally if any player speaks during the sacrifice window â€” speech mechanics are unaffected. However, the Listener **ignores the Token entirely** for the 30-second duration and remains locked on the sacrificing player regardless of who holds the Token or who speaks. The lock is absolute. After the 30 seconds expire, the Listener re-targets the current Token holder as normal.

**Simultaneous use edge case:** If The Loud activates their stun (`F` key) at the same moment another player activates Vocal Sacrifice (`G` key), both effects apply sequentially: the stun freezes the Listener for 5 seconds first, then the Vocal Sacrifice lock takes over for the remaining 30-second window. The 30-second countdown pauses during the stun and resumes when the freeze ends. **The Loud's cooldown in simultaneous edge case:** The Loud's 90-second cooldown begins at the moment the stun is activated (`F` key press), not when its effect ends. In the simultaneous stun + Vocal Sacrifice scenario, the cooldown starts immediately, meaning The Loud can stun again at 90 seconds from the original activation â€” 55 seconds after the combined 35-second disruption window ends.

**Grief detection and Vocal Sacrifice (v1.4):** The grief log for Vocal Sacrifice misuse (Â§13.6) triggers when a player speaks within 2 seconds of the **lock activating** â€” i.e., after they have completed the G-hold and spoken loudly to trigger the lock. It does not trigger during the 1-second pre-signal hold phase. **The activation speech itself is exempt from the 2-second grief detection window** regardless of its duration â€” the 2-second window applies only to speech that begins **after** the activation speech has ended and the countdown has started. The **2-second grief detection window** is measured in **countdown time**, not wall-clock time â€” it begins the moment the 30-second countdown starts ticking (i.e. after the stun ends, if a stun was active). This prevents players from being flagged for legitimate speech during a stun freeze that overlaps with a lock activation.

**Design note:** Vocal Sacrifice is the game's most dramatic mechanic. It requires a player to volunteer themselves as bait, signal their intention via the pre-signal, and trust their teammates to use the window effectively. When it works, it feels heroic. When it fails â€” usually because a teammate panics and speaks during the window â€” it feels like genuine betrayal.

> **Phase 3 broadcaster restriction (see Â§7.3):** During Phase 3 Permanent Frenzy, Vocal Sacrifice cannot redirect the Listener. The broadcaster activating Vocal Sacrifice on themselves provides no benefit, and teammate activations are disabled during the active broadcast window. If the broadcaster is the only surviving player, Vocal Sacrifice is unavailable.

### 3.7 Silent Communication â€” Gesture System & Controls Reference âš‘

Because speaking is dangerous, players have 8 standard keybind gestures for silent coordination, plus additional gestures unlocked by The Mute role and one environmental mechanic. This section is the authoritative controls reference for all non-movement player inputs.

#### Standard Gestures (All Players)

| Key | Gesture | Meaning |
|---|---|---|
| `Z` | Point forward | "Go this way" |
| `X` | Wave | "Come here" |
| `C` | Thumbs up | "I'm okay / confirmed" |
| `V` | Shake head | "No / danger" |
| `B` | Hand to ear | "I hear the Listener" |
| `N` | Hold up note | "I found a word / dropped a note at the board" |
| `M` | Point at self | "I'll do it / take the Token" |
| `L` | Cross arms | "Stay back / stop" |

> **v1.4 note on `N`:** The gesture now covers both "I found a word" and "I dropped a note at a Registration Board" â€” the same gesture communicates both states. Players can disambiguate using the map marker (`J`, spectator key reserved for dead players) or by following up with a point-forward (`Z`) toward the board's floor. A dedicated "note dropped at board" gesture is a post-launch quality-of-life candidate once the Mute role's usage patterns are better understood from Early Access data.

#### Radial Gesture Wheel (v1.4)

Holding `Middle Mouse Button` (default; remappable) for 0.3 seconds opens a circular overlay centred on the crosshair. The 8 standard gestures are arranged clockwise from 12 o'clock in the same order as the keyboard row (Z at top, proceeding clockwise). The player drags in the direction of the desired gesture and releases to trigger it.

- Total interaction time from hold to release: approximately 0.8â€“1.2 seconds
- Single-key shortcuts remain active simultaneously â€” players who memorise the keys never need the wheel
- The wheel produces **no sound** and causes **no Token transfer**
- Dismissed without triggering a gesture if the player releases without dragging past a 20-pixel deadzone
- Each segment displays the gesture name and a small icon as the cursor hovers it
- The second ring of the radial wheel is only rendered for players who have selected The Mute role. Non-Mute players who scroll the mouse wheel while the radial is open receive no response â€” the scroll input is ignored
- Available on all floors and in all phases

**Design note:** The radial wheel solves discoverability for new players and random-matchmaking sessions where gesture fluency cannot be assumed. It is low implementation cost (a UI overlay with no game-state side effects) and does not replace the keyboard system â€” it sits alongside it. Particularly valuable for The Mute, whose 5 bonus keys (1â€“5) are harder to memorise than the 8-gesture home-row layout.

#### Environmental Sound Mechanic

| Key | Action | Effect |
|---|---|---|
| `Q` | Clap / sound pulse | Illuminates room geometry for 0.5 seconds. Emits sound at **15% of calibrated baseline**, placing it in the Tier 0 (sub-whisper) range with a 4-metre Listener detection radius. Dangerous only if the Listener is within 4 metres. Available to all players. Active only after Lights Out (25-minute mark). **Cooldown: 12 seconds.** |

**Design note on the Clap:** The Clap is functionally only useful when a player's torch battery is depleted â€” an active torch makes the 0.5-second Clap illumination redundant. Players with active torches have no incentive to use the Clap and incur its Tier 0 sound risk unnecessarily. The HUD cooldown bar only appears after a Clap is used, not passively â€” a player who never uses the Clap will not see the indicator. The Clap is a navigation tool with a Tier 0 sound cost. Using it in a room where the Listener is within 4 metres will draw it to Alerted state. Players must weigh brief visibility against proximity risk. This decision becomes more interesting after Metric A adaptation shrinks the whisper safe zone.

#### Role Ability Keys

| Key | Role | Action | Status |
|---|---|---|---|
| `F` | The Loud | 5-second stun pulse. 90-second cooldown. | MVP |
| `T` | The Echo | Replay a teammate's last voice clip as a decoy. 60-second cooldown. | **Post-launch** |
| `R` | The Static | Deploy white noise bubble. 2 charges per run. | MVP |
| `G` (hold 1s) | All players | Activate Vocal Sacrifice. | MVP |

> **v1.4 note:** The Echo's `T` key is **unbound in Early Access**. It is listed here for completeness. The Status column is authoritative â€” do not implement `T` until the Echo role ships post-launch.

#### Interaction Keys

| Key | Action |
|---|---|
| `E` (hold 2s) | Pick up note / Mute Silent Drop on Registration Board |
| `E` (hold 3s) | Barricade door with nearby furniture |
| `ESC` | Open pause / settings menu (game continues â€” see Â§13.1) |

> **Interaction priority (v1.7):** If a Mute is within interaction range of both a note and a Registration Board simultaneously, **note pickup takes priority** â€” you must carry the note before you can drop it. The Registration Board `E` only becomes active when the Mute is carrying a note and within range of a board.

#### Spectator Keys (Dead Players Only)

| Key | Action |
|---|---|
| `J` | Place a temporary skull marker on the full map, visible to all living players. Markers last 15 seconds. 1 active marker at a time. No sound signature. |

> **v1.4 note:** `J` is unassigned for all **living** players and is reserved exclusively for the dead-player spectator map-marking function. It does not appear in the living player controls reference and cannot be remapped to a living player action. This resolves the prior ambiguity where dead-player marking was documented in Â§9.2 but absent from the authoritative controls table.

#### All Keys at a Glance

| Key | Living player | Dead player |
|---|---|---|
| `Z` | Gesture: point forward | â€” |
| `X` | Gesture: wave | â€” |
| `C` | Gesture: thumbs up | â€” |
| `V` | Gesture: shake head | â€” |
| `B` | Gesture: hand to ear | â€” |
| `N` | Gesture: hold up note | â€” |
| `M` | Gesture: point at self | â€” |
| `L` | Gesture: cross arms | â€” |
| `Q` | Clap (Lights Out only)* | â€” |
| `E` | Interact / barricade | â€” |
| `F` | The Loud stun | â€” |
| `R` | The Static bubble | â€” |
| `G` (hold) | Vocal Sacrifice | â€” |
| `J` | â€” | Map skull marker |
| `1`â€“`5` | Mute bonus gestures only | â€” |
| `MMB` (hold) | Radial gesture wheel | â€” |
| `ESC` | Settings overlay | Settings overlay |

> *\* Available only after the 25-minute Lights Out event.

Gestures are **visible at close range only** (within 6 metres) and produce no sound. All keys listed here are **fully remappable** via Settings > Controls (see Â§11.3). Exception: `J` cannot be remapped to a living-player action slot.

> **Hold-to-activate note (v1.4):** Hold-to-activate inputs (`G` for Vocal Sacrifice, `MMB` for the radial wheel) require a button capable of registering sustained hold input. Mouse buttons are valid remapping targets for these actions, but the remapping screen displays a warning if the player assigns a hold action to a button that their system reports as non-analog: *"This button may not support hold detection on all hardware. Test before running."*

**Gesture tutorial:** The full gesture vocabulary is demonstrated during the 3-minute orientation mode (see Â§16.1). A gesture reference card is accessible from the ESC menu at any time during a run, displayed as a small silent pop-up that does not pause the game.

---

## 4. The Listener â€” Enemy Design

### 4.1 Design Intent

The Listener is not a jump-scare machine. It is a **persistent, adaptive, intelligent presence** that learns how your group communicates and responds accordingly. It should feel like it understands you specifically, not like a generic horror monster running a fixed script.

Visually: tall, thin, humanoid, completely white, no facial features except hollow eye sockets that glow in different colours depending on state. Movement is smooth and deliberate. It does not run unless triggered. The contrast between its calm movement and the chaos it causes is intentional.

### 4.2 AI State Machine âš‘

> **Targeting precedence:** The state summaries below describe normal state behaviour. If a state transition, sound response, or target selection conflicts with the Listener priority table in Â§4.2.1, Â§4.2.1 is authoritative.

#### State 1: Idle (Patrol)
- Follows pre-defined patrol routes through the manor
- Routes vary slightly each run (randomised waypoint order within floor zones)
- Emits a soft, low hum audible within 12 metres â€” players learn this sound and fear it
- Eye sockets: **dim white**
- Hearing sensitivity: normal (Tier 0 at 4m, Tier 1 at 8m, Tier 2 at 20m, Tier 3 unlimited)
- **Transition to Alerted:** Any Tier 2+ sound detected within detection radius. Tier 1 sounds trigger orientation behaviour but do not force a state transition (see Â§3.3).
  - **Transition to Hunting:** Direct transition from Idle is rare; usually requires a Tier 3 scream.

- **State 2: Alerted**
   - Movement speed: 1.0Ã— (walk). Orients toward sound source immediately.
   - Eye sockets: **amber glow**.
   - Behaviour: Moves to last known position of sound. Does not investigate hiding spots â€” this behaviour begins in Hunting state.
   - **Transition to Hunting:** If the player is spotted or makes a Tier 2+ sound within 5 metres. After the 10-minute mark, Alerted transitions to Hunting on a 15-second timer regardless of silence (see Â§8.1).
   - **Transition back to Idle:** After 8 seconds of total silence (minute 0â€“10 only). Tier 1 sounds **pause the 8-second countdown for 2 seconds** but do not restart it from 8. This allows brief whispers during Alerted but prevents keeping the Listener in Alerted state indefinitely via constant whispering. **Cumulative pause cap:** Tier 1 pauses are limited to a maximum of 6 seconds total per Alerted event. Once 6 seconds of pause have been consumed, further Tier 1 sounds provide no additional delay. **Tier 0 sounds do not pause the 8-second Alerted-to-Idle countdown** â€” only Tier 1+ sounds pause it. The Noise Box (Tier 0) does not extend Alerted state duration â€” it only sustains the Listener's presence at the source location. After minute 10, the Noise Box holds Alerted for at most 15 seconds before Hunting escalation. (see Â§3.3).

#### State 3: Hunting
- Movement speed: 1.3Ã— (fast walk / jog)
- Eye sockets: **red glow**
- Navigates systematically toward Last Word Token holder's position
- **If no Token exists:** navigates toward the last detected sound source; in Phase 3, navigates toward the radio's physical location (see Â§3.1). Returns to Alerted if no further input within 15 seconds.
- Checks rooms in sequence â€” opens doors, investigates hiding spots
- Any **Tier 2+** sound while Hunting: immediately transitions to Frenzy. Tier 1 sounds during Hunting trigger orientation behaviour only (see Â§3.3) and do not cause Frenzy.
   - **Frenzy trigger:** Tier 2+ speech detected while Listener is in Hunting state also triggers Frenzy.
   - If Token holder moves silently out of range for 20+ seconds: returns to Alerted

#### State 4: Frenzy
- Movement speed: **1.8Ã—** (sprint)
- Eye sockets: **white, pulsing**
- Triggered by: scream, Final Broadcast phase start, Tier 2+ speech while in Hunting state, and other explicit priority-table cases. *(Phase 3 Frenzy is permanent and does not use the standard timer â€” see Â§7.3. Phase 3 Frenzy is exempt from the "Heard Nothing" achievement condition â€” see Â§15.1.)*
- Breaks through unlocked doors without animation delay
- Frenzy targeting is resolved by the Listener target priority table in Â§4.2.1. Scream Frenzy locks onto the screamer; Phase 3 Permanent Frenzy follows Token/radio targeting.
- Duration: **12 seconds** (extends to 20s if players screamed frequently earlier â€” adaptive, post-launch). **In Early Access MVP, Frenzy duration is always 12 seconds â€” the adaptive extension to 20 seconds is a post-launch Metric C feature and should not be implemented until the Adaptive system ships.**
- After Scream Frenzy ends: resolve the Listener's next target by re-running the priority table in Â§4.2.1, beginning with any active Vocal Sacrifice lock, then Token, then sound investigation. If no valid target exists, return to Alerted.

### 4.2.1 Listener Proximity, Vision, and Sound Revision Plan

> **Implementation status:** Planned revision. The current prototype uses a smaller attack range and simpler proximity checks; this section defines the intended replacement behaviour before implementation.

**Authoritative targeting priority:** When multiple Listener rules could apply at once, resolve them in this order:

1. **Phase 3 Permanent Frenzy:** Target the broadcaster/Token holder. If no Token exists, target the radio location. This cannot be interrupted by normal screams, Vocal Sacrifice, Static bubble, Silence Room, or sprint retargeting.
2. **Scream Frenzy:** Target the first detected screamer for the standard **12-second Frenzy** window. Other sounds, later screams, Token changes, and Vocal Sacrifice do not retarget the Listener during this lock. If the target dies before the 12 seconds end, Frenzy ends early or falls back to the Token holder if one exists.
3. **Vocal Sacrifice Lock:** Target the sacrificing player for 30 seconds. This overrides Token targeting, normal Hunting, Alerted, non-Frenzy sprinting, and sprint retargeting, but it does not override Phase 3 Permanent Frenzy or active Scream Frenzy.
4. **5m Attack / Vision Kill:** Any living player inside the 5-metre attack range can die if they move, speak, run, land, or make sound. Token ownership is irrelevant. Silent stationary players are safe indefinitely unless a scripted room or hiding-spot check explicitly reveals them.
5. **Second Listener Imprint Target:** The second Listener targets the living player with the highest cumulative speaking time. If that player is unavailable or dead, it falls back to Token holder; if no Token exists, it falls back to last detected sound or Phase 3 radio.
6. **Last Word Token:** Default primary target in normal play.
7. **Sound Investigation:** For player movement noise and non-voice player sounds, under 8m creates wary awareness only, while 8m+ lets the Listener investigate the heard area. Whispered voice remains governed by Tier 1 rules in Â§3.3 unless it is also paired with movement noise. Normal/loud detected sound causes a non-Frenzy sprint to the heard location when it is detected at the 20m threshold or by an explicit long-range/special source. Non-Frenzy sprinting may retarget once per second to newer valid 8m+ movement/noise evidence.
8. **Static Bubble / Silence Room:** These block audio detection according to their own rules but do not suppress Token transfer, do not interrupt active Scream Frenzy after the scream has already been detected, and do not block Phase 3 Permanent Frenzy.

**Design goal:** The Listener should punish careless proximity even when the Last Word Token points somewhere else, while still allowing stationary players to hide in plain sight if they make no sound and do not move.

**Attack range:**
- Listener attack range becomes **5 metres**.
- If any living player enters the 5-metre attack range and is eligible to be perceived, the Listener kills that player immediately.
- Attack range is **Token-agnostic**: the player does not need to hold the Last Word Token to die.
- Walking inside the 5-metre attack range always makes the player eligible to be perceived, so a walking player in attack range is attacked and dies.
- If a player is completely stationary inside the 5-metre attack range and does not speak or make any sound, the Listener does not attack them. They remain safe until they move, speak, run, land, or otherwise emit a qualifying sound.

**Vision rule:**
- Vision is used for immediate danger, not only Token pursuit.
- Any moving player inside the Listener's vision range/cone can be targeted even if another player holds the Token.
- Stationary players are visually ignored unless another signal reveals them.
- A player who does not move and does not make sound remains visually safe indefinitely, even inside the Listener's vision range/cone.
- Implementation should track a small movement threshold so camera jitter, network correction, or idle animation does not count as movement.

**Sound-to-investigation rule:**
- Below **8 metres**, player movement noise and non-voice player sounds can still make the Listener wary: it may turn, pause, or enter a heightened awareness cue, but does not pathfind directly to the sound source from that sound alone.
- At **8 metres or greater**, player movement noise and non-voice player sounds let the Listener record the heard area and move to investigate that location.
- This rule modifies player-made footstep/movement/noise investigation. It does not change Tier 1 whispered voice behaviour in Â§3.3: whispered voice still creates orientation/eye-glow feedback without pathfinding unless another movement/noise signal also qualifies.

**Normal/loud sound sprint rule:**
- At the **20-metre detection threshold**, or from an explicit long-range/special source, a qualifying normal/loud sound causes the Listener to sprint to the heard location rather than walk.
- Scream/loud sound uses this sprint response immediately. Normal speech detected at the 20-metre threshold or by a special source also makes the Listener sprint to the heard noise location.
- This is a movement-response override: the Listener goes to the sound location, even if it has not visually confirmed the player.
- If the sound source is later invalid or the player goes silent, the Listener should investigate the last heard location rather than magically track the current position.
- While sprinting outside Frenzy, the Listener performs a hearing priority check once per second. If it hears valid **8-metre-or-greater** movement/noise evidence within its hearing rules, that newer heard area takes priority over the current sprint destination.
- This sprint retargeting rule means a non-Frenzy sprint is not locked forever: new 8m+ movement/noise evidence can redirect the Listener before it reaches the original heard location.

**Scream-first chase rule:**
- The first player to scream becomes the Listener's active chase target immediately.
- A scream overrides normal Token targeting for the initial chase response.
- During this scream chase, the Listener pursues the screaming player regardless of who currently holds the Last Word Token.
- The scream chase uses the standard **12-second Frenzy** window.
- During Frenzy, the Listener locks onto that player and ignores all other players and all other sounds until the Frenzy ends or the target dies.
- If multiple screams occur close together, implementation should use the earliest valid scream event as the chase owner until the 12-second Frenzy resolves or the target dies.
- Vocal Sacrifice, Static bubble visibility, Silence Room safety, Token transfer, and sprint retargeting do not interrupt an active Scream Frenzy after the scream has already been detected.

**Implementation checklist:**
- Change Listener attack range tuning from the prototype value to **5m**.
- Add player movement-state/perception checks: stationary, walking, running, landing, speaking.
- Separate **visual perception** from **audio investigation** so standing still in sight can be safe while moving in sight is dangerous.
- Add a current scream target field with timeout/clear conditions.
- Add a once-per-second hearing priority tick during non-Frenzy sprinting so valid 8m+ movement/noise evidence can retarget the Listener.
- Ensure Frenzy disables retargeting and remains locked to the scream target until timeout or target death.
- Implement the authoritative target priority table before adding new Listener exceptions.
- Update death handling so a 5-metre valid attack triggers the same death/spectator flow as other Listener kills.
- Add debug labels/logging for: vision spotted, ignored stationary player, attack range kill, heard-area investigation, sprint-to-sound, and scream-target override.

### 4.3 Adaptive Evolution System

The Listener analyses player behaviour across tracked metrics and modifies its own parameters every 5 minutes. **Only the single most dominant metric triggers an evolution at each interval.** This prevents the Listener from simultaneously countering every communication strategy.

In the event of a tie between two metrics at the 5-minute check, the metric that has been elevated longest takes priority. If both elevated simultaneously in the same interval, **Metric A (whisper frequency) takes precedence** over all others, followed by B, C, then D.

**Escalation event priority:** When an Adaptive check interval coincides with a fixed escalation event (minutes 10, 20, 25, 30), the escalation event applies first, then the Adaptive check evaluates. The second Listener (spawning at minute 20) inherits whatever Adaptive parameters were active at the moment of the minute-20 check â€” it does not retroactively inherit prior Adaptive changes.

Adaptation **resets between runs**. It is a within-session complication, not a persistent punishment system.

> **Scope note:** The Adaptive Evolution System is a **post-launch feature** (see Â§16.1). In Early Access, the Listener runs fixed parameters. This section documents intended post-launch behaviour.

**Metric A: Whisper Frequency**
If players whisper >60% of their speech time, the Listener upgrades its hearing sensitivity. Whisper detection threshold drops from 35% baseline to 22% baseline. Safe whisper radius shrinks from 8m to 5m.

**Metric B: Silence Duration**
If the group maintains silence for >2 minutes total, the Listener reduces Playback trigger time and begins patrolling near puzzle locations rather than random rooms.

**Metric C: Scream Frequency**
If players scream more than 4 times, the Listener extends its Frenzy duration by 2 seconds per additional scream and becomes partially desensitised to normal speech.

**Metric D: Ping/Text Usage**
If players use the in-game ping system or text chat more than 8 times *after the 20-minute mark*, the Listener begins investigating recent ping locations with a 15-second delay. Emergency pings are exempt.

**Interaction with the 20-minute Duplication event:** If Metric D was already active before minute 20, the second Listener inherits the ping-investigation behaviour immediately on spawn. If Metric D triggers after spawn, both Listeners investigate pings.

### 4.4 Voice Mimicry â€” Playback Details

> **Scope note:** Post-launch feature. Documented for design intent only.

The Listener's Playback system uses Godot's `Microphone` API to capture and store audio clips:
- Maximum **5 seconds** per clip
- Stored for the **3 most recently spoken** clips per living player
- Selected randomly when Playback activates from the chosen player's 3 stored clips (see Â§3.5 for which player is targeted)
- Processed through distortion filter (pitch shift Â±15%, reverb, slight delay) in standard mode
- Played without distortion in 30% of activations once a Strong vocal profile exists
- Played from the Listener's current position in 3D space
- Clips are sourced only from **living players** â€” dead players' stored clips are discarded as their imprint profile decays (see Â§3.4)
- **Clip prioritization:** The Playback system prioritizes clips from conversational speech over single-word clips (Phase 1 registration or Phase 2 sequence words). If only single-word clips exist in a player's stored pool, the Playback system selects the longest available clip regardless of recency. If all stored clips are single words, the Playback trap still activates but with reduced effectiveness â€” this is a known trade-off and not a bug.

### 4.5 Catch & Death Sequence

When the Listener reaches a player:

1. Grab animation â€” Listener's arm extends and pulls player toward it
2. Screen fades to black over 2 seconds
3. The dead player's **last recorded voice line** plays back, distorted and slowed
4. Death card appears: cause of death, how long they held the Token, their total speaking time, and a first-run tip (see Â§11.5)
5. Player transitions to Whisper spectator mode

The cause of death description is contextual and often darkly comedic:
- *"Spoke too soon."*
- *"Asked if anyone was there. Now no one is."*
- *"Screamed. The Listener agreed."*
- *"Said 'I think we're safe.' They were not."*
- *"The word was worth it. Their life was not."*
- *"Gave their voice so others could live. Others did not notice in time."*
- *"Answered themselves."*

---

## 5. Player Roles

Roles are selected at lobby. Each player chooses one. No duplicates allowed. With 3 MVP roles for up to 4 players, one player may select **No Role** (baseline state) if no roles are available. **No Role** provides no passive ability and no active ability â€” detection radii are standard (same as listed in Â§3.3), and no puzzle restrictions apply. Roles affect passive abilities only â€” no visible indicator to other players beyond what the player chooses to communicate. Players must communicate their role verbally (which costs them the Token) or via gesture.

> **Disconnect mid-lobby edge case:** If a player disconnects after selecting a role and before the run begins, their slot reopens with that role available again. If a player disconnects *during a run*, their role becomes inactive â€” its passive effects and ability charges are lost. No role reassignment occurs mid-run. This is communicated via a brief on-screen notification.

> **No Role option:** Players with No Role can still activate Vocal Sacrifice (G key) as it is a universal mechanic, not a role ability. Token holding and transfer follows standard rules.

### The Mute âš‘
**Passive:** Voice detection radius permanently halved (20m â†’ 10m, 8m whisper â†’ 4m whisper, Tier 3 unlimited â†’ 30m). The Mute's halved detection applies to all tiers, including Tier 3 screams which are capped at 30 metres rather than unlimited.
**Restriction:** Cannot trigger any puzzle that requires voice. Cannot read notes aloud to register them.
**Tier 1+ speech and imprinting (v1.5):** The Mute's restriction is behavioural (cannot trigger puzzles), not a hard mic-mute. If a Mute player physically speaks at Tier 1 or higher, their voice **does contribute to vocal imprinting** (Â§3.4) and **does transfer the Token**. This means a Mute who accidentally speaks above whisper volume can still accumulate a profile and draw second Listener attention. The Mute's reduced detection radius protects them from the first Listener, but their imprint profile is not zeroed â€” they must remain genuinely silent to avoid the second Listener's targeting. This is an intentional risk: the Mute trades puzzle utility for detection safety, but voice discipline is still required.
**Phase 1 contribution â€” Silent Drop:** The Mute can carry a found note to any **Registration Board** (one fixed board per floor on Floors 2 and 3 â€” the Floor 1 board is non-functional for Silent Drop as no notes spawn on Floor 1, and backtracking to Floor 1 is not an intended use of the mechanic). Dropping the note on the board (hold `E` for 2 seconds, silent) makes it available for any teammate to register by reading from it.

> **Floor 4 / Clock Tower note (v1.4):** There is no Registration Board on Floor 4. Phase 1 note collection is expected to conclude on Floors 2 and 3 before players ascend to the tower. The Mute cannot Silent Drop at the tower. If a note has not been registered before ascent, a non-Mute player must descend to register it or the team proceeds with fewer words (triggering Phase 2 scaling â€” see Â§7.2).

**Notifying teammates of a dropped note (v1.4):** When the Mute **successfully completes a Silent Drop** (`E` hold completion at a Registration Board), a one-time map ping automatically appears on all living players' screens â€” a brief shimmer icon appears on the board's floor indicator without requiring players to open any map. **In-game map format:** The in-game map (accessed by dead players automatically, and shown briefly for Mute drop pings) is a 2D schematic per floor, switchable between floors using the `Tab` key. Living players see the Mute drop ping as a shimmer icon on the floor indicator tab corresponding to the board's floor â€” they must switch to that floor's schematic to see the precise board location. Since there is one Registration Board per floor on Floors 2â€“3, the floor indicator alone identifies the board's location uniquely. The Mute should also use `N` ("I found a word / dropped a note at the board") and `Z` (point forward toward the board's floor) to communicate the drop location. A dedicated "note at board" notification is a post-launch quality-of-life target.

**Bonus gestures:** The Mute has access to **5 additional gesture commands** beyond the standard 8, assigned to the following keys:

| Key | Gesture | Meaning |
|---|---|---|
| `1` | Point at floor (direction indicator) | "Listener was last seen this direction" |
| `2` | Hold up one finger | "Word 1 collected" |
| `3` | Hold up two fingers | "Word 2 collected" |
| `4` | Hold up three fingers | "Word 3 collected" |
| `5` | Hold up four fingers | "All 4 words collected" |

> **Cumulative gesture logic:** Keys `2`â€“`5` indicate cumulative progress: key `2` = "at least 1 word collected," key `3` = "at least 2 words," key `4` = "at least 3 words," key `5` = "all 4 collected." Players can convey partial progress by using the highest key that's true. This resolves ambiguity between "I have word 4" and "all words are collected."

These keys (`1`â€“`5`) produce no gesture animation for non-Mute players and are inactive for all other roles. They are remappable via Settings > Controls.

**The Mute and Vocal Sacrifice:** The Mute can activate Vocal Sacrifice. The activation speech is classified as Tier 2.5 (not Tier 3) and does not exceed the Mute's halved detection radii for Tier 2 events (10 metres instead of 20). The Mute's reduced detection does not prevent the lock from activating â€” the lock mechanic is independent of detection radius.
**Accidental Mute Tier 3 scream:** An accidental Mute Tier 3 triggers Scream Frenzy only if detected within the Mute's 30m Tier 3 cap. Outside 30m, it transfers the Token but does not trigger Listener detection or Frenzy. It does not satisfy Phase 2 word registration or Phase 3 broadcast requirements. The Mute's puzzle restriction is unchanged by amplitude.

### The Loud âš‘
**Active ability (`F` key):** Emit a 5-second stun pulse â€” **classified as a special Tier 2.5 ability event** â€” it transfers the Token to The Loud and triggers the 5-second freeze, but does not cause Tier 3 side effects (no screen flash, no physics shatter, no Frenzy sprint). 90-second cooldown.
**Passive downside:** Cannot whisper. The Loud's speech is **always classified as at least Tier 2 regardless of actual amplitude** â€” even quiet speech registers as normal volume for detection purposes. This forced tier override means the Loud cannot benefit from whisper-radius safety and is always detectable at the 20-metre Tier 2 radius. **Phase 2 word registration:** The Loud's forced-Tier-2 passive applies to Listener detection and Token transfer only â€” it does not satisfy the Phase 2 sustained-Tier-2 amplitude requirement. Phase 2 word registration reads raw microphone amplitude. The Loud must genuinely speak at Tier 2 (100%+ baseline) to register their Phase 2 word, the same as all other players. Their passive does not allow whispered Phase 2 word registration.
**Post-stun:** Instantly becomes Token holder after using ability. **After the 5-second freeze ends, The Loud receives a 5-second repositioning window before the Listener re-locks onto them as Token holder.** During this window the Listener resumes normal Token tracking â€” meaning it IS targeting The Loud (as Token holder) but at its normal Hunting speed, not Frenzy speed. The phrase "re-locks" refers to the Listener re-acquiring The Loud as its primary target after the stun freeze, not a suspended tracking period. The window gives The Loud time to reposition before the Listener closes distance, not immGodot from tracking. If another player speaks and takes the Token during the freeze or repositioning window, The Loud is no longer targeted. This makes the stun a genuine emergency tool rather than a delayed death sentence. **Imprinting note:** The Tier 2.5 classification applies only to Token transfer and Listener target-lock â€” the stun does NOT contribute to vocal imprinting (Â§3.4). The Loud does not silently accumulate a Strong imprint profile through ability use alone.
**Death note:** When The Loud dies, their `F` stun is permanently lost for the run. The on-screen role-disconnect notification informs surviving players immediately.
**Design note:** The Loud is the team's emergency button but pays for it constantly. They are always louder than they want to be.

### The Echo âš‘
**Active ability (`T` key):** Replay any teammate's last recorded voice clip as a directional decoy, originating from any room the Echo has previously visited this run. 60-second cooldown.
**Passive downside:** Listener builds Echo's vocal profile at 2Ã— speed. After 10 seconds of speaking, Echo is treated as if they spoke for 20 seconds in the imprinting system.
**Death note:** When The Echo dies, their decoy ability is lost. Any previously deployed decoy continues playing until it expires naturally.
**Design note:** Echo has the highest skill ceiling. Effective Echoes redirect the Listener across the manor. Ineffective Echoes become the Listener's secondary priority faster than any other role.

> **Scope note:** The Echo role is a **post-launch feature** (see Â§16.1). Its `T` key is unbound in Early Access.

### The Static
**Active ability (`R` key):** Deploy a 4-metre radius white noise bubble centred on self. Duration: 40 seconds. Charges: 2 per run.
**Effect:** Players inside the bubble can speak at any volume without Listener detection. **Token transfer still occurs normally inside the bubble** â€” speech transfers the Token regardless of whether the Listener can detect it. The bubble suppresses detection, not the Token mechanic itself. This means the Token can rotate rapidly inside the bubble without the Listener knowing who holds it, creating strategic uncertainty when the bubble ends.

**Design note:** 2 charges Ã— 40 seconds = 80 seconds total bubble coverage per run. This is intentionally scarce â€” roughly 4% of a 35-minute run. The bubble is an emergency coordination tool, not a sustained safe zone. If playtesting shows groups are burning both charges in Phase 1 and having none for Phase 3, consider reducing charge duration to 30 seconds or requiring a cooldown between charges rather than adding a third charge.
**Bubble expiry:** When the bubble expires, the Listener immediately targets the current Token holder with no grace period. Players should plan to transfer the Token to a designated safe holder or create distance before the 40-second window ends. There is no transition buffer on bubble expiry.
**Death interaction:** If The Static dies while a bubble is active, the bubble collapses immediately. Any remaining charges are lost for the run. The on-screen role-disconnect notification informs surviving players, consistent with other role death notifications.
**Drawback:** The bubble is visible to the Listener as a faint luminous distortion in the air. The Listener cannot hear through it but can *see* it and will navigate toward it during Hunting state. **During Frenzy:** The Listener ignores the bubble's visual distortion. During Scream Frenzy it stays locked to the screamer; during Phase 3 Permanent Frenzy it navigates toward the Token holder or radio location regardless of bubble position. The bubble's audio-blocking effect still applies in Frenzy, but it will not alter an already locked Frenzy path.
**Audio inside the bubble:** VC audio is *not* muffled. Players can hear each other clearly.
**Visual detection range:** The Static bubble is visible to the Listener from **15 metres** during Hunting state. After Lights Out (minute 25), ambient darkness makes the luminous distortion **more visible** â€” the glow stands out against the dark background, and the Listener can detect it from **20 metres** instead. Players should factor increased bubble visibility into deployment decisions after minute 25.
**Design note:** The bubble creates a safe speech zone that is also a beacon. Groups must decide whether the coordination value outweighs the positional exposure.

### The Archivist
**Passive:** Can read coded word notes without speaking â€” notes register silently when the Archivist holds them for 5 seconds.
**Restriction:** Must still speak the word aloud at the Clock Tower during Phase 2. Only the collection phase is silent.
**Bonus:** Sees a faint shimmer on walls near unregistered word notes (within 8 metres).
**Design note:** The Archivist breaks Phase 1 risk almost entirely but cannot escape Phase 2 and Phase 3 exposure.

> **Scope note:** The Archivist role is a **post-launch feature** (see Â§16.1).

### The Witness
**Post-death passive:** Ghost burst duration extended from 3 seconds to 6 seconds per burst.
**Tradeoff:** Each ghost burst creates a louder static signature â€” Listener hears it as 20% mic volume equivalent (vs 10% for standard ghosts).
**Additional ability:** Can see the Listener's exact path for the next 10 seconds after death, and relay this via burst.
**Design note:** The Witness makes the spectator phase genuinely meaningful. A skilled Witness can save a run. A Witness who overuses bursts can doom it.

> **Scope note:** The Witness role is a **post-launch feature** (see Â§16.1). Ghost burst spectator is also post-launch.

---

## 6. Map Design â€” Ashford Estate

### 6.1 Design Principles

- **Single building** â€” manageable scope for solo dev, justified by horror genre conventions
- **Vertical layout** â€” four floors with a single main staircase creates natural bottlenecks and routing decisions
- **Each floor has a distinct identity** â€” visual palette, acoustic properties, and unique mechanic
- **No dead ends** â€” every room on Floors 1â€“3 has at least two exits. The Clock Tower has a single staircase entrance â€” an intentional single-entrance kill zone for Phase 2/3 tension.
- **Small enough to feel claustrophobic, large enough to lose the Listener temporarily**

**Base player movement speed:** Walk = 4 m/s, Run (hold Shift) = 6 m/s. All relative speed multipliers in this document reference the base walk speed unless otherwise stated. The Listener's 1.0Ã— speed equals player walk speed (4 m/s); 1.3Ã— (Hunting) = 5.2 m/s; 1.8Ã— (Frenzy) = 7.2 m/s. Dead players move at 2Ã— walk speed (8 m/s). These values are starting points for playtesting and may be adjusted.

### 6.2 Floor 1 â€” The Basement

**Visual identity:** Exposed stone, water-stained walls, a furnace in the southwest corner
**Function:** Player spawn zone, Listener spawn zone
**Unique mechanic:** Creaking floor tiles â€” running through the central corridor creates sound at **Tier 0 (15% of calibrated baseline)**, detectable within 4 metres. Players must walk (hold Shift) through this area to avoid detection.
**Key items:**
- Broken intercom near the furnace (voice amplified to entire floor when spoken near)
  - **Player perception:** floor-wide audio broadcast.
  - **Listener detection:** Tier 2 event at the intercom's physical position only (20-metre radius). The Listener does not treat this as a floor-wide alert â€” it only responds if within 20m of the intercom.
- Lockbox containing one item per run (randomised from item pool)
- Staircase hub â€” the only access point to all upper floors
- **Registration Board** (non-Mute registration only â€” the Mute's shimmer vision does not reveal this board, as no notes spawn on Floor 1. The board remains usable by non-Mute players who find a note and wish to register it near the staircase hub.)

**Listener patrol note:** The Listener always begins here. Its first patrol route covers the basement perimeter before ascending. Players who move immediately and quietly can gain significant distance.

### 6.3 Floor 2 â€” The Bedrooms

**Visual identity:** Faded floral wallpaper, dusty furniture, moonlight through broken windows
**Function:** Primary item cache, 2 of 4 coded word note locations
**Unique mechanic:** Creaking floorboard corridor â€” the hallway between rooms 2A and 2E has a persistent creak zone. The Signal Jammer (Â§10.1) can temporarily silence this zone.
**Key items:**
- Dead phones in rooms 2B and 2D (**each phone has one use per run; both phones can be used in the same run by different players or the same player** â€” **interaction is UI-driven:** approaching the phone displays a list of living teammates; selecting one sends them a location hint. Your voice broadcasts to entire floor as **Tier 2 at the phone's position** for Listener detection purposes. Each phone sends a one-time location hint to the selected teammate. There is no restriction on targeting the same teammate from both phones, but each use is a separate Tier 2 event at its respective phone position.
   - **Player perception:** floor-wide audio broadcast.
   - **Listener detection:** Tier 2 event at the phone's physical position only (20-metre radius). The Listener does not treat this as a floor-wide alert â€” it only responds if within 20m of the phone.
   - Each phone is single-use per run. Both phones can be used in the same run, but each is a separate Tier 2 event at its own physical position. There is no shared cooldown â€” using one phone does not lock out the other. **Random matchmaking note:** In public lobbies where players may not know each other's names, the phone UI displays each teammate's Steam display name for selection.)
- **Wardrobe in room 2A** â€” hiding inside mutes all outgoing sound for up to 20 seconds but the player cannot move or see outside.
   - **Listener Interaction:** The Listener can open and investigate wardrobes during Hunting and Frenzy states. If a player is inside, the Listener **opens the wardrobe door (no delay, visible to all players in the room)** and then initiates a **grab animation (0.5s)** identical to a standard grab.
   - **Note:** Lit candles in this room affect the Listener's patrol pathfinding (Â§10.2) but do not prevent wardrobe investigation â€” the Listener checks the wardrobe regardless of candle confusion.
   - **20-second limit:** After 20 seconds, the wardrobe doors do not auto-open, but the **sound suppression expires**. Any sound made inside after the limit is detected as if the player were in the open.
   - **Exit Risk:** Exiting while the Listener is **within 5 metres and in Hunting or Frenzy state** triggers immediate detection, regardless of facing direction. **During Frenzy specifically,** the Listener's grab animation (0.5s) completes faster than a standard exit â€” do not attempt to exit the wardrobe while the Listener is actively Frenzying in the same room. Exiting is silent otherwise.
- Coded word notes 3 and 4 (randomised locations within this floor each run)
- **Registration Board** (Mute drop point)

**Acoustic note:** Bedrooms are small. The Listener's 20-metre detection radius covers most of a single bedroom at normal speech volume. Players on this floor should default to whisper.

### 6.4 Floor 3 â€” The Library âš‘

**Visual identity:** Floor-to-ceiling bookshelves, reading tables, a fireplace (unlit, ambient)
**Function:** Puzzle hub, Silence Room location, 2 of 4 coded word note locations
**Unique mechanic:** The Silence Room â€” a soundproofed study off the main hall. VC is completely muted while inside. Listener cannot detect players inside. Entrance is visible and the Listener knows to check it during Hunting state. **Audio isolation:** The Silence Room suppresses all audio â€” both incoming VC and in-world audio events (intercom broadcasts, Listener hum, Gramophone, environmental sounds). Players inside the Silence Room are fully audio-isolated in both directions. Intercom broadcasts on Floor 3 do not reach players inside the Silence Room. **Frenzy exception:** The Silence Room provides no protection during Frenzy once the Frenzy target is already established. During Scream Frenzy the Listener follows the screamer; during Phase 3 Permanent Frenzy it enters freely if the Token holder or radio is inside or beyond it. **Broadcaster note:** The broadcaster cannot use the Silence Room during Phase 3 â€” entering it immediately applies the VC mute, which triggers the 2-second silence penalty and freezes the broadcast timer. The Listener also enters the Silence Room freely during Phase 3 Permanent Frenzy. The Silence Room offers no benefit to an active broadcaster.

**Silence Room capacity rules (v1.3):** The Silence Room holds a **maximum of 2 players simultaneously**. A third player attempting to enter receives a silent visual prompt: a crossed-out door icon. This prevents the Silence Room from functioning as a whole-team safe house.

**Silence Room 2-player run rule (v1.4):** In a 2-player run, both players occupying the Silence Room leaves nobody free to create a distraction. **The two-condition Aggressive Check trigger (standard version, above) applies with reduced thresholds:** the Listener must be within 10 metres of the entrance AND 25 seconds must have elapsed since the last detected audio event near the Library floor. When Aggressive Check triggers at 25 seconds, the Listener stations at the door AND a **forced-exit prompt** appears simultaneously on both players' screens: *"The Listener has found you. One of you must move."* The prompt gives players a 10-second window to exit voluntarily before the Listener breaks through. If neither exits within 10 seconds of the prompt appearing, the Listener **breaks through the Silence Room door** and enters the room. This creates real stakes â€” ignoring the prompt is no longer a safe bluff. The maximum station time is reduced to 20 seconds in 2-player sessions (down from 60 seconds). Note: the 10-second prompt window overlaps with the 20-second maximum station time, meaning if players ignore the prompt entirely, the break-through occurs before the station time fully elapses.

**Aggressive Check behaviour (standard):** Aggressive Check requires **two conditions simultaneously**: (1) the Listener is **within 10 metres of the Silence Room entrance**, AND (2) the Listener's last detected audio event was within 20 metres of the Library floor and more than 45 seconds have elapsed since. A Listener that has not recently detected audio near the Library does not trigger Aggressive Check from proximity alone â€” it must have reason to suspect the area is occupied. When triggered, the Listener stations itself at the Silence Room entrance for up to 60 seconds, blocking the exit. **Station behavior:** The Listener stands in the doorframe of the Silence Room entrance. The Listener does not enter the room during station â€” it blocks the exit from outside. Any player who exits while the Listener is stationed triggers immediate detection (same as the wardrobe 5-metre exit rule: Â§6.3) â€” the Listener initiates a grab animation (0.5s) on the exiting player regardless of Token status. Players must wait for the Listener to be drawn away by a distraction before safely exiting. Players must either wait it out or create a distraction elsewhere to draw it away. Aggressive Check does not apply during Phase 3 (the Listener is in permanent Frenzy and does not station). In 2-player sessions, the trigger threshold is reduced to 25 seconds and the maximum station time to 20 seconds (see 2-player rule above). **Break-through consequence:** Once the Listener breaks through the Silence Room door, the door is permanently destroyed for the remainder of the run â€” the room no longer provides sound isolation. The Listener exits the room and resumes its normal AI state after break-through. Players cannot restore the Silence Room's function mid-run. **Two-Listener interaction:** Only one Listener can trigger Aggressive Check at a time. If the first Listener is already stationed at the Silence Room entrance, the second Listener does not independently trigger a second Aggressive Check â€” it continues its normal targeting behavior (Token holder or highest cumulative speaker). Players can draw the second Listener away as a distraction to allow exit past the stationed first Listener.

**Key items:**
- Broken intercom mounted on the wall adjacent to the Silence Room entrance
   - **Player perception:** floor-wide audio broadcast. The Floor 3 intercom broadcast does not reach players inside the Silence Room â€” the Silence Room's full audio isolation blocks all in-world audio events including intercom broadcasts.
   - **Listener detection:** Tier 2 event at the intercom's physical position only (20-metre radius). The Listener does not treat this as a floor-wide alert â€” it only responds if within 20m of the intercom.
- Coded word notes 1 and 2 (randomised locations within this floor each run)
- Bookshelves create line-of-sight breaks
- **Registration Board** (Mute drop point)

**Strategic note:** Floor 3 is where most runs are decided. The combination of puzzle objectives, the capped Silence Room, and the central intercom means players must make the most consequential communication decisions here.

### 6.5 Floor 4 â€” The Clock Tower âš‘

**Visual identity:** Gothic spire, exposed clock mechanism, a single large window overlooking the grounds
**Function:** Win condition location, Final Broadcast radio
**Unique mechanic:** Bell rings every 5 minutes â€” creates 4 seconds of ambient noise that masks speech entirely. **Bell schedule:** The bell first rings at minute 5, then every 5 minutes (5, 10, 15, 20, 25, 30). The bell ring at minute 20 coincides with the second Listener spawn â€” this is intentional. Players who hear the bell during the Duplication event may not hear the second Listener's initial movement. Escalation events are not audio-announced beyond their mechanical effects. Players who time a brief communication during a bell ring take no detection risk. **Token transfer note:** Speaking during the bell ring transfers the Token normally â€” the bell masks Listener detection but does not suppress Token transfer. Players who speak during a ring become the Token holder and will be targeted when the bell ends. **Orientation mode communication:** The bell's Token-transfer interaction is explained in orientation mode (Â§16.1 segment 6). A death card tip exists for players who die within 10 seconds of a bell ring ending: *"The bell masks Listener detection â€” but it doesn't stop the Token from transferring. Whoever spoke during the ring was already marked."*
**Key items:**
- The radio (Final Broadcast trigger)
- Spiral staircase â€” single entrance, defensible but also inescapable if the Listener follows
- No permanent hiding spots â€” the tower is open and exposed
- No Registration Board (Phase 1 collection concludes on Floors 2â€“3; see Â§5 Mute role note)

**Phase 2 evasion option (v1.3):** The spiral staircase has a **heavy door at the tower entrance** (top of the staircase, before the clock mechanism area). This door can be barricaded from the inside using the standard furniture barricade mechanic (hold `E` for 3 seconds). It holds for **12 seconds** before the Listener breaks through â€” 4 seconds longer than standard doors, reflecting the weight of the door. This gives players a brief window to complete a word or two before needing to flee down the stairs and return. The door resets (unbarricaded) once the Listener breaks through or stops attempting.

**Second Listener targeting note:** After the 20-minute mark, the second Listener spawns and targets the loudest living player. It does **not** run a fixed patrol. Its arrival at the tower is a consequence of targeting logic, not a scripted route.

---

## 7. Objective System â€” Three Phases

### 7.1 Phase 1: Find the Words (0â€“15 minutes)

**Objective:** Locate and register all four coded word notes hidden across floors 2 and 3.

**Note placement:** Randomised each run from a pool of 12 possible locations (6 per floor). Never placed in the same room as a major item.

**Registration process:**
- Pick up the note (interaction: hold `E` for 2 seconds â€” silent)
- Read the word aloud into VC â€” the word appears on a registry screen visible to all players
- **Minimum tier for registration:** Tier 1 (whisper) is sufficient. Unlike Phase 2 words, Phase 1 registration does not require sustained Tier 2 amplitude. A player may register a note at whisper volume, transferring the Token but remaining at lower detection risk.
- The speaking player immediately receives the Last Word Token
- **Death mid-registration:** Phase 1 word registration is instantaneous on valid speech detection (Tier 1+, any duration). If a player begins speaking a registered word and is grabbed simultaneously, the word registers if the speech event was detected before the grab animation initiated (i.e., the Token transfer and registry update happen server-side on speech detection, not on speech completion). The grab animation (0.5s) does not retroactively cancel a speech event already processed.
- Archivist role can register by holding the note for 5 seconds (silent) â€” post-launch
- Mute role can carry the note to a Registration Board for a teammate to read from there

**Word pool:** 90 possible coded words across two tiers, documented in full in the separate Word Pool asset file. Tier 1 (default): monosyllabic. Examples: "Ash," "Bone," "Dusk," "Veil," "Stone," "Hark," "Grim," "Fell," "Wane," "Rift." Tier 2 (higher difficulty / Lights Out): two-syllable words harder to whisper cleanly. Examples: "Hollow," "Stillness," "Threnody," "Fracture," "Wither." The complete 90-word pool is maintained as a separate asset file, not solely implied by these examples.

**Phase 1 failure state:** None. The escalation system pressures players indirectly by making the manor more dangerous over time.

> **Intentional word-skip strategy:** Skipping a word registration is permitted but triggers Phase 2 scaling â€” players may intentionally skip words to reach Phase 2 faster with a smaller required sequence. This is an accepted risk/reward trade between Phase 1 safety and Phase 2 difficulty reduction, not an exploit. Design note: players who ascend early without all words proceed with fewer required words in Phase 2, accepting higher individual risk per speaker.

### 7.2 Phase 2: Speak the Sequence (15â€“25 minutes)

**Objective:** Surviving players converge at the Clock Tower and speak the registered words in the correct sequence.

**Sequence reveal:** Correct order displayed only after all required words are registered and the players have ascended to the tower. **Sequence order generation:** The correct sequence order is randomized per run at session start, independent of the order notes were found or registered. The sequence is stored on the host machine and distributed to all clients on registration completion. Players have no way to determine the correct order before reaching the tower â€” deliberate, as it prevents pre-planning the sequence during Phase 1. The sequence display shows all registered words with numbered positions (1st, 2nd, 3rd, 4th) and each player's assigned word. Players who arrive at the tower before all words are registered must wait â€” the sequence display is gated on registration completion. Players should complete all registration on Floors 2â€“3 before ascending; ascending early and waiting is permitted but leaves teammates undefended below. **Early ascent risk:** Players waiting at the tower before sequence display activates have no objectives and no safe hiding locations â€” early ascent is a calculated risk, not a recommended strategy. The `Z` (point forward) + `M` (point at self) gesture combination can serve as an improvised "I'm at the tower" signal, but this is not formally defined. See orientation mode (Â§16.1 segment 6) for the recommended Phase 1 completion strategy.

**Player count scaling:**
- 4 surviving players: all four words, one per player, all within 10 metres of the clock mechanism
- 3 surviving players: three words required; fourth automatically skipped
- 2 surviving players: two words required

**10-metre radius definition:** The 10-metre Phase 2 radius is measured from the center of the clock mechanism to the player's position (capsule center). The clock mechanism occupies approximately 3 metres of the tower floor's center space. During level design, confirm that the 10-metre radius covers the entire tower floor â€” the tower room is approximately 12 metres in diameter, so players near the walls remain within range. The radius is indicated visually by a faint floor glow when the sequence is active.

**Speaking rules:** Each word: minimum 0.5 seconds duration, **sustained Tier 2 amplitude (100%+ baseline) for the full duration**. A single-frame peak above 100% is insufficient. Word registration requires continuous Tier 2 throughout the 0.5-second window.

**Word registration scaling (v1.5):** Phase 2 word count is determined by **the lower of: surviving player count OR registered word count**, with no enforced floor â€” a group that registered 1 word proceeds with a 1-word sequence (one speaker, extreme risk, intentional trade). The sequence display gate only blocks ascent if *zero* words are registered. **1-word skip survival dependency:** The 1-word skip only functions if at least 2 players survive to Phase 2. If the surviving player count drops to 1 before the sequence completes, the single-survivor fail state fires regardless of word count. Players who skip to 1 word accept both the individual speaking risk and the dependency on at least 2 survivors reaching the tower.

**Extra players in Phase 2:** If the required word count is lower than the surviving player count (e.g. 4 players, 2 words required), the "extra" players who have no word to speak are **exempt from the 10-metre break condition**. They may move freely or act as lookouts outside the radius. **Extra players inside the radius are present cosmetically only â€” they cannot trigger a sequence break.** Only players assigned a word in the current sequence must remain within the **10-metre clock mechanism radius** to maintain sequence integrity. If a designated speaker leaves the radius, the sequence breaks.
- 1 surviving player: **Fail state** (see below)

**Single-survivor fail state (v1.4):** The fail state triggers when the number of surviving players drops to one, regardless of session size. In a 4-player run this occurs when the third player dies. In a 3-player run, when the second player dies. In a **2-player run, when the first player dies.** The fail state is session-size aware â€” it fires at "one survivor remaining," not at a fixed death count. **The fail state triggers on death event, not on tower arrival.** The sequence display never renders for a solo survivor â€” the check happens server-side on each death.

When the fail state triggers: the Phase tracker displays a red X over the Phase 2 dot. The message reads: *"The sequence requires more than one voice."* A 10-second grace period countdown appears. If the last player dies during this grace period, the run ends immediately with a full-party-wipe screen (see Â§9 for the total wipe screen spec).

**Total party wipe:** If all players are dead simultaneously at any point in the run â€” including during the Phase 2 grace period â€” the session ends immediately. A distinct end screen appears: *"The estate is silent. No voice remains."* This is separate from the single-survivor fail state screen and has no grace period.

**Text Broadcaster and Phase 2 (v1.4):** If the only surviving player capable of speaking is a Text Broadcaster (Â§7.3), and their partner dies during Phase 2 ascent, the single-survivor fail state triggers immediately â€” Text Broadcaster mode does not count as a "voice" for Phase 2 word-speaking purposes. The fail state message remains the same. This outcome should be disclosed in the Text Broadcaster mode description in Settings > Accessibility.

**Dead player contribution (post-launch):** Post-launch, a dead player can contribute their word via ghost burst delivered within 3 metres of the clock mechanism. Static signature emits at the clock, not the ghost's position.

**Sequence rules:**
- All required surviving players within 10 metres of the clock mechanism
- **Word amplitude requirements:** see Speaking rules above (sustained Tier 2 for 0.5 seconds)
- Wrong order: sequence resets, mechanism locks for 30 seconds
- Listener reaches tower during Phase 2: sequence pauses, players must evade and return (see barricade option Â§6.5)

### 7.3 Phase 3: The Final Broadcast âš‘

**Objective:** One player speaks a 10-second monologue into the radio while others protect them.

**Broadcast trigger:** After Phase 2 completes, the radio activates. The broadcaster approaches and begins speaking.

**Listener state during Phase 3:** The Listener enters **permanent Frenzy for the entire broadcast phase** â€” it does not use the standard 12-second Frenzy timer (see Â§4.2). The Frenzy ends only when the broadcast is completed (victory) or all players are dead. Each broadcaster handoff does not reset the Frenzy â€” it is continuous from Phase 3 start to end.

**Monologue system:** A randomised text appears on the broadcaster's screen. Content drawn from a pool of 80 options:
- **60 dramatic/tense texts** â€” gothic prose, fragmented journal entries, estate history
- **20 absurdist texts** â€” mundane or comic content delivered under maximum pressure. Example: *"Item number four on this week's shopping list: eggs, milk, bread, and one large tin of beans..."*

The pool weighting: 75% dramatic, 25% absurdist â€” a random draw each run. Players may receive absurdist texts in consecutive runs; the distribution is per-run probability, not guaranteed rotation.

**Broadcaster death and radio handoff:**
- Broadcaster caught mid-broadcast: drops radio to floor at death location
- Radio has a **1-second pickup delay** after dropping
- Next broadcaster gets a **2-second grace window** after pickup before the silence penalty activates
- The Listener does not interact with the radio as a physics object
- Timer resets to 0 when a new broadcaster picks up the radio
- **Design note on handoff timer:** The broadcast timer resets to 0 on each broadcaster pickup. There is no progress memory across handoffs â€” the new broadcaster must complete the full 10-second monologue. This is intentional to keep pressure high when rotations occur.
- Any surviving player can be the next broadcaster

**Radio pickup interaction and grab race condition (v1.4):** A player can attempt to pick up the radio even if the Listener is in the same room. The pickup interaction requires a 1-second `E` hold. The Listener's grab animation completes in 0.5 seconds (Â§9.1). This means a player who begins the pickup while the Listener is already within grab range will always lose the race â€” the grab completes before the pickup. This is **intentional**: the radio cannot be rescued from an active grab attempt. Players must create distance or wait for the Listener to disengage before attempting pickup. This should be communicated in a death-card tip for players who die attempting to pick up the radio mid-grab. **Radio break during pickup:** If the radio breaks while a pickup interaction is in progress (E hold not yet completed), the interaction cancels and the radio becomes non-interactable. If the pickup completed before the break, the broadcaster retains the radio but it deactivates mid-broadcast â€” treating this as an immediate broadcast failure.

**Vocal Sacrifice during Phase 3 (v1.4):** During the active broadcast window, Phase 3 Permanent Frenzy has top targeting priority and Vocal Sacrifice cannot redirect the Listener. The broadcaster activating Vocal Sacrifice on themselves provides no benefit, and teammate activations are disabled while the broadcast is active. If a teammate already activated Vocal Sacrifice before Phase 3 Permanent Frenzy begins, the Phase 3 priority overrides the remaining sacrifice lock.

**2-player Phase 3:** One broadcasts, one defends. Listener's sprint speed during Phase 3 Frenzy reduced by 15% in 2-player sessions. The defending player cannot redirect Phase 3 Permanent Frenzy with Vocal Sacrifice; their role is to use barricades, timing, and movement to buy the broadcaster time.

**Broadcast rules:**
- Broadcaster stops speaking for more than **2 seconds**: timer freezes, Listener immediately re-locks
- The Listener enters **full Frenzy** for the entire broadcast duration
- Surviving players must physically block doors, activate Vocal Sacrifices, deploy role abilities

**ESC menu during broadcast:** When the broadcaster is in an active broadcast (timer running), pressing ESC displays a single-line overlay *over* the broadcast UI: *"Broadcast paused â€” Listener re-locking. Close menu to resume."* The game does not freeze. The 2-second silence rule immediately applies â€” the timer freezes and the Listener re-locks as designed. The overlay makes the consequence legible so the broadcaster knows what they've triggered. **Host broadcaster note:** If the host player is the Phase 3 broadcaster and opens ESC, the Listener AI continues running on their machine. The 2-second silence penalty applies immediately as normal. Host broadcasters should be warned in the lobby screen that opening ESC during Phase 3 triggers the silence penalty regardless of host status.

**Listener target during Phase 3 with no Token:** The Listener targets the Token holder if a Token exists. If no Token exists, the Listener targets the radio's physical location (see Â§3.1 for the canonical no-Token targeting rule).

**Text Broadcaster mode â€” Non-speaking player accessibility (v1.3):**
A player who cannot use a microphone (mute, non-speaking, or with a hardware fault) can enable **Text Broadcaster** mode from Settings > Accessibility before the run. In this mode, during Phase 3 only, the broadcaster's radio interaction displays the monologue text on screen and the player types it using a keyboard input field. The game synthesises the typed text as a neutral voice for the Listener's detection purposes. The broadcaster's timer runs at **0.6Ã— speed** (10-second monologue takes 16.7 seconds to type) to account for typing speed. The Listener still enters Frenzy. The synthesised voice counts as Tier 2 detection for the entire broadcast regardless of player input speed. **Text Broadcaster mode produces a continuous synthesised Tier 2 audio signal for the duration of active typing with no gap between keystrokes. The 2-second silence penalty only triggers if the player stops typing for 2 or more consecutive seconds.**

Text Broadcaster mode cannot be used for Phase 1 note reading or Phase 2 word speaking â€” those require in-game VC from a living player. The mode is Phase 3 only. This is a deliberate partial accessibility option, not a full silent playthrough option. This limitation â€” including the Phase 2 single-survivor fail consequence â€” must be disclosed on the Steam store page in the accessibility section and within the Settings > Accessibility description of the mode.

**Victory condition:** Broadcaster completes the full monologue. Estate shakes, exit unlocks, all surviving players win. Dead players receive a win regardless.

---

## 8. Escalation & Pacing

### 8.1 The Escalation Timeline

The run deteriorates on a fixed timer regardless of player progress.

**0â€“10 minutes â€” Baseline**
One Listener. Standard detection parameters. Playback activates at 90-second silence (post-launch). Listener builds adaptive profiles (post-launch). Players learn the estate and find notes. Tension: moderate and building.

**10-minute mark â€” Silence ImmGodot**
The Listener's Alerted state 8-second Idle reset is **suspended**. Alerted now transitions directly to Hunting on a **15-second timer** regardless of silence. The 15-second timer begins the moment the Listener enters Alerted state. Alerted states that began before the 10-minute mark and are still active at minute 10 immediately begin the 15-second Hunting transition countdown. The two-timer system (8-second reset before / 15-second Hunting transition after) is explicitly sequential, not simultaneous â€” the 8-second reset simply stops applying after this mark.

**20-minute mark â€” Duplication**
A second Listener spawns in the **Basement (Floor 1)**, mirroring the original Listener's start position. Its arrival at the tower is a consequence of its targeting logic, not an instant threat â€” players on upper floors have time to hear it ascending. **Staircase traversal:** The Listener (and second Listener) navigates between floors via the single main staircase using standard NavMesh pathfinding. Staircase traversal time at Hunting speed (1.3Ã— = 5.2 m/s) from Basement to Floor 3 is approximately **25â€“30 seconds** â€” determined during playtesting. The hum remains audible through ceilings at 50% its normal range, giving players on upper floors advance warning of the Listener ascending. The original tracks the Last Word Token holder. The clone tracks the **living player with the highest cumulative speaking time** â€” **this draws directly from the imprint profile table (Â§3.4)**, specifically the cumulative speaking-time values used to determine profile strength. The player with the highest value in that table is the second Listener's primary target. If the highest-cumulative-speaker is dead at spawn time, the second Listener falls back to Token-holder targeting (same as the first Listener) until a living player surpasses 30 cumulative seconds of speech, at which point it locks onto that player. **If no Token exists at the time the second Listener's fallback activates**, the second Listener uses the same no-Token rule as the first Listener (Â§3.1): it navigates toward the last detected sound source, or in Phase 3, toward the radio's physical location. Two threat vectors with different targeting logic. **30-second fallback design note:** The 30-second fallback threshold is an edge case designed for near-silent runs (e.g., all-Mute groups or extreme whisper discipline). In typical play, at least one player exceeds 30 cumulative seconds before minute 20 â€” the second Listener's primary targeting will almost always be active on spawn. The fallback exists to prevent undefined behavior, not to describe expected gameplay.

> **Mute role interaction:** Assuming the Mute player maintains genuine silence, they will never be targeted by the second Listener's primary targeting logic, effectively making them immune to one of the two Listeners at the 20-minute mark. However, per Â§5, if a Mute speaks at Tier 1+ volume, they *do* accumulate speaking time and can lose this immGodot if they surpass the 30-second threshold. This is a known role asymmetry and an intentional design choice â€” the Mute trades voice utility for reduced threat exposure in the late game.

**25-minute mark â€” Lights Out**
All fixed lighting cuts permanently. Players carry torches with 90-second battery life. Replacement batteries found throughout the estate. Clap mechanic (`Q`) unlocks for echolocation â€” 0.5 seconds of room geometry illumination at the cost of a Tier 0 sound event (15% calibrated baseline; 4-metre detection radius).

**30-minute mark â€” The Final Deadline**
A 3-minute countdown begins. If the Final Broadcast is not completed within 3 minutes, a third Listener spawns and the radio permanently breaks. This is not communicated until the countdown appears â€” the timer's appearance at 30 minutes is itself a horror moment. **If the broadcast completes during the 3-minute countdown window, the third Listener spawn is cancelled and the win condition fires normally.** **Third Listener and Vocal Sacrifice:** The third Listener spawning at minute 30 is not affected by any active Vocal Sacrifice lock â€” the lock applies only to the Listener that was targeted when the lock activated. The third Listener uses standard targeting logic (Token holder) from spawn. Groups relying on Vocal Sacrifice at the 30-minute mark face the third Listener without protection.

**Session abandonment cooldown adjustment:** Abandoning within 60 seconds of the 20-minute Duplication event (minute 20:00â€“20:45) incurs the 5-minute cooldown. This prevents exploit of leaving at the highest-pressure moment after securing significant session progress. The no-cooldown threshold remains at minute 20 for abandonments after the Duplication spawn concludes.

### 8.2 Pacing Philosophy

- Minutes 0â€“10: exploratory and tense but manageable
- Minutes 10â€“20: increasingly dangerous as silence stops working as a strategy
- Minutes 20â€“25: genuinely threatening with two Listeners
- Minutes 25â€“30: desperate with darkness and time pressure converging
- The Final Broadcast always happens under maximum pressure

---

## 9. Death & Spectator System

### 9.1 Death Sequence

When a player is caught by the Listener:

1. **Grab animation:** 0.5-second ragdoll grab, screen lurches
2. **Fade to black:** 2 seconds of darkness and rising audio distortion
3. **Voice playback:** The player's last recorded voice line plays back at 60% pitch, with heavy reverb â€” 3â€“5 seconds
4. **Death card:** Cause of death text, Token hold time, total speaking time, severity rating, and a first-run tip (see Â§11.5)
5. **Transition:** Player enters Whisper mode

**Total party wipe screen:** If all players are dead simultaneously, the session ends with a distinct screen showing all four death cards side by side and the message: *"The estate is silent. No voice remains."* No grace period applies. This screen is separate from the Phase 2 single-survivor fail state screen. **Phase 2 grace period death flow:** If the last surviving player dies during the Phase 2 grace period, the sequence is: individual death card (standard sequence above) â†’ total party wipe screen. The Phase 2 single-survivor message and countdown are dismissed when the death card appears. The party wipe screen follows the death card with a 1-second delay. The Phase 2 fail state message does not appear separately â€” it is superseded by the death card when the final player dies.

### 9.2 Whisper Mode (Spectator) âš‘

Dead players become Whispers â€” translucent observers with limited agency.

**MVP scope:** Ghost bursts are a **post-launch feature**. In Early Access, dead players use standard spectate view plus silent map marking only.

**Whisper abilities â€” MVP:**
- See the full estate map including real-time Listener positions (both, after duplication)
- Move freely through walls at 2Ã— normal player movement speed
- **Dead players phase through all geometry (walls, floors, doors, furniture) except the Listener's collision space. The Listener is the only entity that maintains a collision boundary with dead players.** Dead players are not bound by floor geometry â€” they move freely in all three dimensions. Vertical movement is unrestricted; dead players can pass through ceilings and floors to navigate between floors without using the staircase. Dead players **phase around the Listener** at a **2-metre buffer** â€” they cannot be moved directly through the Listener's collision space. This prevents griefing via walking a ghost into the Listener during an active hunt and confusing living players about its position. **Auto-push rule:** Dead players who remain stationary within 2 metres of the Listener for more than 5 consecutive seconds are automatically pushed to 3 metres. This prevents passive visual obstruction without penalising players moving through the area.
- **Silent map marking:** Press `J` to place a temporary skull marker on the full map, visible to all living players. Markers last 15 seconds. Limited to 1 active marker at a time. No sound signature.
- **ESC menu access:** Dead players can access the ESC menu overlay from spectator view. The overlay appears over the spectator camera. The J-key map marking function is suspended while the ESC menu is open. The game continues running normally.
- **Phase 3 broadcast timer visibility:** Dead players in spectator view can see the Phase 3 broadcast timer. The timer appears in the same screen position as for living players. Dead players can use J-key markers to guide the broadcaster toward safety but cannot intervene directly.

> **Key note (v1.4):** `J` is **entirely non-remappable** â€” locked to its dead-player map-marking function with no option to move it. In the remapping screen, `J` does not appear in the living-player controls list. In the dead-player controls list, it appears greyed out with the label: *"Reserved â€” cannot be remapped."* See the Controls Reference in Â§3.7 for the complete key assignment table.

**Whisper abilities â€” Post-launch addition:**
- Emit ghost bursts: **3-second VC windows** per burst, **20-second cooldown**
- Each burst creates an audible static sound at the ghost's location â€” Listener perceives it as 10% of calibrated baseline

**Victory share:** Dead players receive a win if the remaining team escapes.

---

## 10. Items & Environmental Interactions

### 10.1 Item Pool (Randomised per run) âš‘

**Randomised Item Pool (spawn locations vary per run):**

| Item | Description | Risk | MVP? |
|---|---|---|---|
| Lighter | Creates a sustained light source for **3 minutes**. **Held in hand**; prevents interaction with objects while equipped. Cannot be placed or dropped. Useful for extended darkness navigation but blocks note pickup. **Exceptions:** Lighting candles (**auto-trigger at 1.5 metres** while lighter is equipped; no button input required, cannot be undone), **note pickup (`E` hold for Phase 1)**, **door barricading (`E` hold, 3 seconds)**, and radio pickup (`E` hold for Phase 3 handoff) are all permitted while holding the lighter. **Interaction restriction applies to `E`-key interactions only. Passive physics collisions (furniture knockovers) still occur normally while the Lighter is equipped.** Faint warmth glow visible to Listener from 6 metres. | Low | Yes |
| Torch Battery | Replacement battery for standard torch. Restores 90 seconds of light. **One battery is guaranteed to spawn per floor** â€” its position within each floor is randomised from a set of 3â€“4 predefined spawn points. **Total of 4** replacement batteries per run (Basement, Bedrooms, Library, Clock Tower). **Clock Tower design note:** The Clock Tower battery is intentionally the highest-risk pickup â€” players who didn't collect it before Phase 3 must retrieve it from an open, exposed floor during active Frenzy. Collecting it during Phase 2 is the intended low-risk window. **Phase 2 battery collection:** Non-speaking players (those not assigned a word in the Phase 2 sequence) may retrieve the Clock Tower battery during Phase 2 without penalty â€” only designated speakers must remain within the 10-metre radius. Players should coordinate battery collection with non-speakers before the sequence begins. | None | Yes |
| Noise Box | Deployable device that emits periodic sound bursts at 25% calibrated baseline (Tier 0, 4-metre radius) from its location. **Emits its first burst 4 seconds after deployment, then every 4 seconds thereafter, for a total of 11 bursts over 44 seconds. The device deactivates at 45 seconds.** Players deploying the Noise Box should account for the 4-second delay before the first burst â€” it is not an immediate distraction. **Causes Alerted state only â€” cannot directly trigger Hunting.** **After minute 10, deploying the Noise Box will reliably cause Hunting escalation within 15 seconds. It is not a safe late-game distraction.** The 45-second duration is the sound emission window. After minute 10, the Alerted-to-Hunting transition timer applies, so the Listener may escalate to Hunting on its own timer while near the Noise Box â€” but this is the natural Alerted-to-Hunting transition, not the Noise Box directly causing Hunting. **Listener arrival behavior:** When the Listener reaches the Noise Box's position and finds no player, it treats the Noise Box as a non-threat environmental sound source and returns to Idle after the standard 8-second Alerted reset (before minute 10) or after the 15-second Hunting transition timer (after minute 10, which means the Listener may escalate to Hunting while near the Noise Box before returning to patrol). The Noise Box is not destroyed or deactivated by the Listener's presence â€” it continues emitting until its 45-second window expires. The Listener does not interact with the Noise Box as a physics object. The item is useful as bait but not a guaranteed distraction. | Medium (positional exposure) | Yes |
| Signal Jammer | **Primary (MVP):** Silences creak sound events for both Listener detection and player audibility for 90 seconds â€” any room's creak zones produce no sound while active. Silent to use. One-time use per run. **Secondary (post-launch):** Also disables Listener's Playback system for 5 minutes when Playback is active. Risk column reflects MVP primary function only; post-launch Playback suppression may carry tactical risk depending on Listener proximity when activated. | None for MVP primary function | Yes |
| Earmuffs | Wearing these makes the bearer immune to Playback trap voice mimicry â€” they hear only static. Cannot hear Listener footsteps either. Post-launch item (Playback dependent). | Medium | No |
| Torch (upgraded) | 4-minute battery. Brighter radius (8m vs 4m standard). Visible to Listener from 10 metres. | Low-medium | Yes |
| Old Gramophone | Found in Library. Playing it creates sustained ambient sound (40% calibrated baseline, Tier 1) at its location for 90 seconds. Draws Listener to Alerted state within 8 metres. **Environmental Tier 1 sounds provide a confirmed location** â€” the Listener navigates to the gramophone's position, unlike player voice at Tier 1 which only indicates presence without location. Can be used as bait. **Tier 1 environmental exception:** Tier 1 sounds from fixed environmental sources trigger Alerted state directly â€” the confirmed position provides sufficient information for the Listener to pathfind. This overrides the Tier 2+ requirement for environmental-source Tier 1 events (see Â§3.3). | Contextual | Yes |

**Fixed / Default Items (not in randomised pool):**

| Item | Description | Risk | MVP? |
|---|---|---|---|
| Torch (standard) | Default torch with 90-second battery. 4-metre light radius. Visible to Listener from 6 metres. **Lights Out distribution:** At Lights Out (minute 25), all players who do not already have a torch (standard or upgraded) receive a standard torch automatically. Players who already equipped an Upgraded Torch before minute 25 do not receive a second torch â€” they retain only the Upgraded Torch. Players who found but did not equip an Upgraded Torch before minute 25 receive the standard default torch; the unequipped Upgraded Torch remains in its found location and can still be picked up. | Low | Default |

**Signal Jammer MVP rationale (v1.3):** The revised primary function â€” creaking floor silence â€” is a meaningful tactical item in all game states, particularly useful on Floor 2's creaking corridor and the Basement's creak tiles. It costs nothing technically (a simple audio flag toggle) and gives the item an identity before the Playback system ships.

### 10.2 Environmental Interactions

All environmental sounds are classified under the Tier 0â€“3 system. Quick reference:

| Interaction | Sound Level | Tier | Detection Radius |
|---|---|---|---|
| Creaking floor tile (running) | 15% baseline | Tier 0 | 4 metres |
| Clap (`Q`) | 15% baseline | Tier 0 | 4 metres |
| Furniture knockover | 30% baseline | Tier 0 | 4 metres |
| Gramophone | 40% baseline | Tier 1 | 8 metres |
| Noise Box burst | 25% baseline | Tier 0 | 4 metres (Alerted only) |
| Dead phone broadcast | 100%+ baseline | Tier 2 | At phone position (20m radius; floor-wide player perception) |
| Broken window wind (persistent) | 5% baseline | Tier 0 | Ambient only â€” no Listener alert |
| Candle lighting | Silent | N/A | No detection â€” proximity trigger only |

> **Environmental vs. player Tier 1 distinction:** Tier 1 player voice causes the Listener's eye sockets to glow faintly blue â€” it senses something but cannot locate the player. However, **environmental Tier 1 sounds (Gramophone, etc.) provide a confirmed location** because they originate from a fixed, non-moving source. The Listener navigates to the sound's position. This distinction allows environmental items to function as bait while preserving the uncertainty of player voice detection at Tier 1.

**Furniture / Physics Objects:** Major objects (chairs, small tables) can be knocked over by running into them. Knockover sound: 30% calibrated baseline, Tier 0 (4-metre detection radius). **Knockover trigger:** Furniture knockover triggers on any player movement collision with a physics object at walk speed or above â€” it does not require sprinting. Players who move carefully around furniture (no collision) avoid the knockover sound. The collision hitbox for knockover is slightly larger than the visual mesh to account for player capsule width.

**Barricade Mechanic (Â§10.2 addition):**
Standard doors can be barricaded using nearby furniture. Hold `E` for 3 seconds near a door with furniture within 2 metres. **Barricade rules:**
- Standard doors: barricade holds for **8 seconds** before the Listener breaks through
- Heavy Clock Tower door: barricade holds for **12 seconds** (reflecting door weight)
- Barricading is silent â€” no Tier classification, no detection risk
- Players can barricade from either side of the door
- A barricade cannot be removed by the barricading player â€” only the Listener's break-through clears it
- **If the Listener initiates a grab animation while a player is mid-barricade, the barricade interaction cancels. The player must reposition before attempting again.**
- Furniture requirement: at least one moveable physics object (chair, small table) within 2 metres of the door
- Each floor's barricadeable doors are defined during level design â€” not every door has nearby furniture

**Windows:** Breakable if the Listener Frenzies through them. Once broken, that room has permanent ambient wind noise (5% calibrated baseline â€” ambient only, no Listener alert).

**Clock Bell (Floor 4):** Rings every 5 minutes. During the 4-second ring, all player-generated sounds are masked.

**Candles:** Scattered throughout the estate. Can be lit with the lighter item (**auto-triggers at 1.5 metres** while lighter is equipped; no button input required, cannot be undone â€” see Lighter exceptions in Â§10.1). **Design warning:** The auto-light trigger at 1.5 metres means Lighter-equipped players must actively avoid close proximity to candles if they do not want the pathfinding confusion effect active â€” e.g., when attempting to let an Alerted Listener pass without redirection. Lighting a candle is silent and causes no Listener detection. **Lit candle effect:** Casts moving shadows that confuse the Listener's pathfinding for **3â€“5 seconds** with **70% probability**. Effect applies only during **Alerted state** â€” Hunting and Frenzy states are unaffected. The confusion causes the Listener to pause briefly or alter its patrol direction, creating a momentary window for evasion. **Wardrobe note:** Candle confusion does not prevent the Listener from opening and checking the wardrobe in room 2A â€” the confusion affects patrol pathfinding, not the specific wardrobe investigation behaviour.

---

## 11. UI & HUD Design

### 11.1 Design Philosophy
The HUD is minimal. Horror is undermined by excessive UI. Players should feel like they are in the manor, not watching a game.

### 11.2 Persistent HUD Elements âš‘

**Last Word Token indicator:** Top-centre of screen. Shows the name of the Token holder in a faint red glow. Pulses faster as the Listener gets closer. **Pulse rate thresholds:** slow pulse at Listener distance >12 metres, medium pulse at 8â€“12 metres, fast pulse at 4â€“8 metres, rapid strobe at <4 metres. These thresholds align with the accessibility proximity pulse activation distance (12 metres, Â§11.3) for consistent player feedback across HUD elements. Disappears entirely when Token holder enters the Silence Room, **except during Frenzy state â€” during Scream Frenzy the indicator shows the locked Frenzy target, and during Phase 3 Permanent Frenzy it shows the Token holder or radio fallback target regardless of Silence Room status.** **Not present at run start** â€” appears only after first speech. **Reappearance after Silence Room exit:** When the Token holder exits the Silence Room, the Token indicator reappears instantly on all players' HUDs with no delay. In a 2-player run where both players are inside the Silence Room, the Token is frozen on its current holder â€” VC muting prevents transfer. The indicator is hidden for both players during this period and reappears for both when the Token holder exits.

**Voice activity indicator:** Bottom-right, glows when the player is speaking. Colour-coded: grey (silent / Tier 0), **light blue (Tier 1 whisper)**, orange (Tier 2 normal), red (Tier 3 loud). The blue Tier 1 HUD colour matches the Listener's Tier 1 visual response (Â§3.3) for consistent player feedback. Shows the player's own tier in real time.

**Bottom HUD layout note:** The three bottom HUD elements occupy fixed non-overlapping positions: battery indicator (bottom-left), Clap cooldown indicator (bottom-centre), and voice activity indicator (bottom-right). Battery and Clap cooldown only appear when their conditions are met, so simultaneous display is rare. If both are active at once, neither moves â€” they coexist in their fixed positions.

**Phase tracker:** Top-left. Three dots representing the three phases. Filled dot = complete. Current phase glows faintly.

**Battery indicator:** Bottom-left, appears only when torch is equipped. Depletes visually over 90 seconds.

**Teammate status:** Four small icons at screen edge showing each player as: alive (white), Token holder (red pulse), dead (grey), in Silence Room (muted icon), role disconnected (faded icon with strike). **During Scream Frenzy, the locked Frenzy target's status icon shows the red pulse state regardless of their location. During Phase 3 Permanent Frenzy, the Token holder or radio fallback target receives the red pulse.** The Silence Room muted icon is overridden for the active Frenzy target.

**Vocal Sacrifice countdown:** When any player activates Vocal Sacrifice, a 30-second countdown appears on all players' HUDs in amber.

**Phase 2 fail state indicator:** When the number of surviving players drops to one (session-size aware â€” see Â§7.2), the Phase tracker displays a red X over the Phase 2 dot and shows the message: *"The sequence requires more than one voice."* The 10-second grace period countdown appears in the same location.

**Clap cooldown indicator:** Small icon at bottom-centre, appears only after the 25-minute Lights Out mark. Shows a brief flash icon and a **12-second cooldown bar** after each `Q` Clap use, so players know when they can safely use echolocation again without spamming. **At minute 25 exactly, a one-time unlock notification flashes the Clap icon briefly in the bottom-centre HUD to indicate the mechanic is now available.**

### 11.3 Accessibility Features & Controls

**Listener proximity pulse:** A subtle screen-edge pulse (slow, warm-coloured) that activates when the Listener is within 12 metres. Supplements â€” does not replace â€” the audio hum.

**Listener direction indicator:** When the Listener is within 8 metres and the proximity pulse is active, a faint directional arc appears at the screen edge. Visible for 2 seconds per update.

**Playback warning indicator (Post-launch):** When the Playback system activates, a brief icon flashes at the bottom of all living players' screens. Deferred to post-launch. Will be toggleable in Settings > Accessibility.

**Subtitle system:** All Listener-generated audio events (hum, click, Playback) are subtitled **above the bottom HUD element row** (centre of screen, positioned higher than the battery/Clap/voice indicators). This prevents overlap with the Clap cooldown indicator when both are active after Lights Out. Player VC is not subtitled (post-launch goal).

**Keybind remapping (v1.3):** All player inputs â€” gestures, role abilities (`F`, `R`, `T`), interaction keys, Clap, Vocal Sacrifice, and the radial gesture wheel â€” are fully remappable via **Settings > Controls**. Role ability keys are included in the remapping system and can be reassigned like any other input. The remapping screen displays: current key assignment, the action's description, a "reset to default" button per key, and a conflict warning if the player assigns a key already in use. Mouse buttons are valid remapping targets. Exception: `J` cannot be remapped to a living-player slot. Hold-to-activate actions (`G`, `MMB`) display the hardware compatibility note described in Â§3.7.

**Text Broadcaster mode (v1.3):** Toggleable in **Settings > Accessibility** before the run. Activates keyboard-typed Phase 3 broadcasting for players who cannot use a microphone. The toggle is visible in the lobby screen so teammates can see if a player is in Text Broadcaster mode before the run begins. The Settings > Accessibility description of this mode must include the Phase 2 limitation: if the Text Broadcaster is the sole survivor entering Phase 2, the single-survivor fail state triggers immediately.

**Non-speaking player note:** Players who are physically unable to speak (mute, non-speaking, or hardware fault) can participate in Phase 3 via Text Broadcaster mode. Phases 1 and 2 still require at least one speaking player per registered word â€” this cannot be bypassed. Text Broadcaster is a partial accommodation for Phase 3 only, not a full silent playthrough option. This must be disclosed on the Steam store page in the accessibility section.

### 11.4 First-Time Setup Screen (v1.5)

First-time setup activates automatically on first launch, before the main menu. It covers the following steps in sequence:

1. **Region selection:** Player selects their preferred region from the list in Â§18.5. Used for matchmaking and Relay server routing. Can be changed later in Settings > General.
2. **Mic calibration:** The full calibration flow from Â§3.2. Mandatory on first launch, cannot be skipped here. Can be re-run later from Settings > Audio.
3. **Accessibility options:** Player is shown three toggles â€” Proximity Pulse, Subtitles, and Text Broadcaster mode â€” with brief descriptions. All off by default. Can be changed at any time in Settings > Accessibility.
4. **Keybind review:** Player is shown the default controls table (Â§3.7 "All Keys at a Glance") with a prompt: *"All keys are remappable in Settings > Controls."* No action required; this is informational.
5. **Privacy notice acknowledgement:** The voice data privacy statement (Â§13.3) is shown in full with an explicit "I understand" confirmation button. Players cannot proceed without acknowledging. This acknowledgement is logged locally and displayed as "acknowledged" in Settings > Privacy. **In Early Access, the privacy notice should clearly state: "Voice recording is not active in this version. This notice describes a planned post-launch feature. Your acknowledgement is recorded for when the feature activates."**

After setup completes, the player proceeds to the main menu. Returning players who have completed setup skip directly to the main menu. All setup values are stored in the local save file (Â§14.5).

### 11.5 Settings Menu Structure

**Settings > General:**
- **Region selection** â€” player's preferred region for matchmaking and Relay routing (changeable post-setup)
- **Language** â€” game language selection (TBD at launch â€” English only for Early Access)
- **Display mode** â€” fullscreen / windowed / borderless (standard Godot options)
- **VSync** â€” on / off
- **FPS counter** â€” toggle for on-screen FPS display (debug/quality-of-life)

**Settings > Audio:**
- **Mic calibration re-run** â€” repeats the full calibration flow from Â§3.2
- **Master volume** (slider) â€” mirrors the ESC overlay audio sliders
- **VC volume** (slider)
- **Listener audio volume** (slider â€” minimum floor 20%, see Â§13.4)
- **Ambient/environmental volume** (slider)

**Settings > Privacy:**
- **Privacy notice status** â€” displays "Acknowledged" with timestamp once the player has confirmed the voice data privacy statement
- **View privacy statement** â€” button to re-read the full privacy notice at any time (required for GDPR compliance â€” players must be able to review consent terms post-acceptance)
- **Revoke consent** â€” not applicable during an active session (voice recording is session-scoped); this button is greyed out during runs and explains that voice data is destroyed at session end regardless. For players who wish to permanently disable voice recording, the only option is to disable the Playback system (post-launch) or avoid using in-game VC entirely.

> **GDPR note:** Under GDPR Article 7(3), consent must be as easy to withdraw as it was to give. The "View privacy statement" button satisfies the right to review. The session-scoped nature of voice data (destroyed at session end, never persisted) means there is no persistent data to delete â€” revocation is automatic. This should be confirmed with legal review before EU launch.

### 11.6 Death Card Tip System

For a player's first 5 deaths across all runs, the death card includes a one-line contextual tip. **Tips are suppressed when either condition is met** â€” after 5 deaths OR after 3 Token-holding runs, whichever comes first. Both conditions independently suppress tips. This prevents tips from appearing to experienced players regardless of their personal death count.

Example tips:
- Died as Token holder: *"The skull above your head marks you as the Listener's target. Speak last â€” hold the risk. Stay silent â€” pass it on."*
- Died from Playback trap: *"The Listener can replay your voice. If you hear a teammate during a long silence, don't respond â€” it may not be them."*
- Died during Vocal Sacrifice window: *"Someone sacrificed themselves to give the team a safe window. During that 30 seconds, their countdown shows on your HUD."*
- Died by screaming: *"Screaming triggers a 12-second sprint. The louder the group plays, the faster the Listener gets."*
- Died attempting radio pickup during active Listener grab: *"The Listener's grab takes 0.5 seconds. The radio pickup takes 1 second. Don't reach for it while they're reaching for you."*
- Died while Token indicator was hidden in Frenzy: *"During Frenzy, the Token indicator is always visible â€” even inside the Silence Room. The Listener always knows where you are. Plan your exits accordingly."*

### 11.7 Non-HUD Feedback

**Listener proximity:** No explicit indicator beyond the accessibility pulse. Players must listen for the ambient hum (audible within 12 metres) or watch for eye glow through gaps in walls or doors.

**Token transfer:** A brief directional audio sting plays for the player who receives the Token â€” a whispered "you" from an indistinct voice.

**Whisper feedback:** Subtle visual vignette around screen edges when in whisper tier.

---

## 12. Audio Design

### 12.1 Audio Mix Validation

**This must be tested in Week 2 of development, before any art or level work begins.**

The game runs two audio pipelines simultaneously: Godot Voice/WebRTC VC (3D positional, player voices) and Godot's audio mixer (Listener soundscape, environmental audio, UI feedback). These pipelines do not automatically integrate.

Target relative levels (establish in Week 2, lock by Month 3):
- Listener ambient hum at 12 metres: clearly audible at the same perceived volume as normal VC speech
- Listener Frenzy tone: louder than all VC, intended to dominate the audio space
- Environmental ambience (wind, creak): quieter than whisper-tier VC

### 12.2 Listener Soundscape
- **Idle hum:** 20Hz foundation, audible within 12 metres. Increases in pitch slightly as Listener accelerates through states.
- **Alerted click:** A single sharp tongue-click sound when Listener enters Alerted state.
- **Hunting breath:** Slow, deliberate breathing audible within 8 metres during Hunting.
- **Frenzy:** All ambient sound drops out. Only the Listener's sprint sound remains â€” a high, sustained tone.
- **Catch:** Total silence for 1 second before the death sequence audio.

### 12.3 Environmental Audio
- **Creaking floors:** 3D positional audio, audible to all players within 15 metres (suppressed by Signal Jammer). Tier 0 for Listener detection purposes (4-metre radius).
- **Clock bell:** Full estate audio broadcast, 4 seconds, masks all other sounds
- **Broken window wind:** Persistent low ambient in any room with broken windows. Ambient only â€” does not trigger Listener alert.
- **Gramophone music:** Crackled, distant â€” Tier 1 for Listener detection. Serves as Listener bait but also creates atmosphere.

### 12.4 VC Audio Processing
Player VC is processed to add subtle reverb matching the acoustic properties of the room they are in. A player speaking in the stone basement sounds different from one in the carpeted bedrooms. This is both atmospheric and functional â€” it gives positional information about speakers.

---

## 13. Multiplayer Architecture

### 13.1 Networking âš‘

- **Godot Multiplayer API** â€” handles player state, object sync, Listener position
- **Host-authoritative model** â€” one player hosts, Listener AI runs on host machine
- **Relay servers** â€” Steamworks.NET / Godot Steam Relay prevents NAT traversal issues
- **Session size:** 2â€“4 players maximum for matchmaking. Private/custom lobbies allow starting with a minimum of 1 player (the host) to facilitate solo play and developer testing.
- **Lobby system:** Steam Lobby service â€” code-based friend joining and public random matchmaking (see Â§18)

**Pause / ESC menu behaviour during live sessions (v1.3):** There is **no pause functionality** in multiplayer sessions. Opening the ESC menu brings up an overlay (settings, scoreboard, audio sliders, vote-kick) while the game continues running in the background. The Listener keeps moving. Players who open the ESC menu while in a dangerous location do so at their own risk. This is standard for multiplayer horror games and should be communicated in the orientation mode. If a player opens the ESC menu while holding the Token, the Token indicator remains active and the Listener continues navigating toward them.

### 13.2 Host Disconnection Handling

The Listener AI running on host hardware means host disconnection ends the session for all players.

**Mitigations:**
- Lobby screen identifies the host by name with a "(hosting)" label
- Host disconnection mid-run: all players receive "Host disconnected â€” session ended" screen (not a generic error)
- Session code preserved for 60 seconds after host disconnection â€” original host can reconnect and resume from last 30-second state snapshot
- If host does not reconnect in 60 seconds, session closes
- **Host crash vs graceful disconnect:** If the host's process terminates without a graceful disconnect signal (crash), other players receive the "Host disconnected" screen after a 10-second timeout (the time Steamworks.NET Relay takes to detect a dead connection). The standard 60-second rejoin window then applies. Players should be advised that a crash-based rejoin may take longer than a graceful disconnect due to the 10-second detection delay.
- **Mic failure mid-run:** If a player's microphone fails mid-run (amplitude reads zero for more than 10 consecutive seconds while their connection is active), a "Mic lost" notification appears on their screen and teammates' status bars. The player is treated as a Mute for the remainder of the run â€” their detection radius is halved but they cannot trigger voice puzzles. The Token is not transferred by mic failure. If they were the Token holder, the Token remains on them until another player speaks. A mic failure does not trigger the grief detection system.
- **Involuntary vs voluntary disconnect distinction:** Involuntary disconnects (detected via Steamworks.NET Relay timeout without a graceful leave signal, such as crashes or network drops) do not incur the session abandonment cooldown (Â§13.6). The abandonment cooldown applies only to players who voluntarily leave via the ESC menu quit option. This prevents penalising players for hardware or connection failures outside their control.

**Public lobby host assignment:** In random matchmaking sessions, the host is the first player who created the lobby. If that player leaves, the session ends per standard rules. Host migration is a post-launch goal.

**Host migration (post-launch):** Transferring Listener AI state to a new client is technically complex. Deferred to Year 1 post-launch milestone.

**Reconnect system:** Any disconnected player can rejoin using the session code within 60 seconds. After 60 seconds, the slot closes. **During a player's 60-second rejoin window, their slot is reserved for that specific player only** â€” it does not appear as open in the public lobby browser. The lobby status remains 'In Run.' Strangers cannot join a mid-run session under any circumstances. **Run-end edge case:** If a run ends (win or loss) while a player's rejoin window is still active, the rejoin window closes immediately and the player receives the run's outcome screen â€” win or loss â€” on reconnect, with their personal stats if they were alive at disconnect. They cannot rejoin a session that has already concluded. **Dead-at-disconnect stats:** Players who disconnected while dead receive the run outcome screen with their personal pre-death stats (Token hold time, speaking time, cause of death) on reconnect. The phrase 'if they were alive at disconnect' refers to live players receiving their full end-of-run stats including Phase 3 contribution; dead-at-disconnect players receive their death card stats only.

### 13.3 Voice Chat âš‘

**Primary SDK:** Godot Voice/WebRTC (Godot's official voice solution, free tier covers indie usage)

**SDK risk mitigation:** Architecture must remain SDK-agnostic at the amplitude-detection layer. The tier classification system reads from a normalised amplitude value (0.0â€“1.0). If Godot Voice/WebRTC is replaced, only the SDK adapter layer changes.

Validate the Godot Voice/WebRTC amplitude API produces consistent, low-latency values in Week 1 before building anything above it.

- **3D positional audio** â€” voice volume attenuates with distance between players
- **Volume detection** â€” Godot Voice/WebRTC provides real-time amplitude data per speaker
- **Recording** â€” Godot `Microphone` API captures and stores clips for Playback system (post-launch)
- **Proximity rules** â€” players beyond 15 metres hear each other at reduced volume; beyond 25 metres, VC is inaudible

**Soundboard and voice spoofing:** Soundboard misuse that spams Tier 3 volume events intentionally may be flagged by the grief detection system. There is no automated voice authentication â€” enforcement relies on grief detection and vote-kick. Known and accepted limitation.

**Voice data privacy and GDPR compliance (v1.3):** The game captures short voice clips from each player during a session for use in the Playback system (post-launch). The following privacy commitments apply and must be reflected on the Steam store page and in a brief in-game privacy notice displayed on first launch (as part of the First-Time Setup screen, Â§11.4):

- Voice clips are stored **in-session only**, in local memory on the host machine
- Voice clips are **never transmitted to the developer or any server**
- Voice clips are **destroyed at session end** â€” they do not persist to disk
- No voice data is processed by third-party AI services
- Players are informed of voice recording at the lobby screen via a persistent one-line notice: *"In-game voice is recorded locally this session for gameplay purposes only."*
- **Adaptive metric tracking:** The Adaptive system's metric tracking (Metrics Aâ€“D: whisper frequency, silence duration, scream frequency, ping usage) runs locally during Early Access even though the Adaptive system is dormant. These metrics are session-scoped, stored in local memory only, and destroyed at session end â€” they are never transmitted. The privacy notice includes: *"In-session behavioral metrics (voice tier frequency, silence duration) are tracked locally for future gameplay features and are not transmitted or stored between sessions."

This applies to the post-launch Playback system. In Early Access, the `Microphone` API is not used (Playback is cut from MVP), so no voice clips are captured. The privacy notice should still be shown in Early Access to establish trust ahead of the feature's introduction.

**GDPR note for EU players:** Under GDPR Article 6(1)(a), voice recording requires consent. The First-Time Setup privacy acknowledgement (Â§11.4) constitutes informed consent for the purposes of this gameplay mechanic. Legal review of this consent mechanism is recommended before the Playback system ships post-launch, particularly for EU storefront availability.

### 13.4 Audio Settings Screen

Accessible from the ESC menu and the lobby screen:

- **Master volume** (slider)
- **VC volume** (slider â€” affects all player voices globally)
- **Listener audio volume** (slider â€” separately adjustable, gameplay-critical; **minimum floor 20%**, tooltip appears below 30%: *"Warning: Listener audio cues are core gameplay information. Reducing below this level may make the Listener undetectable by sound alone."*)
- **Ambient/environmental volume** (slider)
- **Individual player volume** (adjustable per player from the scoreboard overlay)
- **Playback warning indicator** (toggle â€” post-launch feature, greyed out in MVP)

> **Accessibility toggles moved:** Proximity pulse and subtitles are accessibility features and appear in **Settings > Accessibility** (see Â§11.3), not in this screen. They are not duplicated here to prevent implementation confusion.

### 13.5 Latency Considerations
- Listener position synced at 20 updates/second
- Token transfer is client-reported but validated on the host machine. In the host-authoritative model, the host validates all transfers. **When the host is the Token holder, transfer validation runs locally with no round-trip. This is a known edge case of the host-authoritative model with no current mitigation planned.**
- Token transfer uses client-side prediction: transfer shows immediately on speaking player's screen, reconciles with server state
- Voice tier classification happens client-side and is broadcast to all players

### 13.6 Grief Protection

- **Vote-kick:** Requires 3-of-4 (or 2-of-3, 2-of-2) players to confirm within 15 seconds. Kicked players cannot rejoin the same session. **Dead player voting:** Dead players retain their vote-kick vote. The required threshold (e.g. 2-of-3) is calculated from total session player count, not living player count. A dead player's vote counts toward the threshold. This prevents griefers from becoming unkickable after teammates die. **2-player session exception:** In 2-player sessions, the host may unilaterally kick the other player **once per session**. The kicked player receives the standard session cooldown. If the kicked player has not triggered any grief detection logs during that session, the host's kick is flagged for manual review alongside any report. The grief escalation system (below) still applies â€” if the host abuses this, two kicks within 24 hours triggers the public lobby restriction on the host's account.
- **Grief Detection:** The system logs high-frequency Token transfers (e.g., 5 transfers in 3 seconds) and deliberate Vocal Sacrifice misuse (speaking within 2 seconds of the lock activating). **Static bubble exemption:** Token transfers are exempt from the high-frequency log if the **speaking player** is within an active Static bubble radius at the moment of transfer â€” rapid rotation inside the bubble is an intended mechanic (see Â§5). **If a transfer occurs with the speaking player outside the bubble radius, that transfer is logged normally. All speakers must remain inside the bubble during rapid rotation to maintain the exemption.** Three logged instances flag the account for review. **Vocal Sacrifice window note:** The 2-second window is measured in **countdown time** â€” it begins the moment the 30-second countdown starts ticking, not at wall-clock time of lock activation. See Â§3.6 for the stun edge case.
- **Reporting:** A post-run reporting tool is available to all players. Use of the "Griefing" report tag automatically attaches the last 30 seconds of the grief detection logs for manual review.
- **Session abandonment cooldown:** Abandoning before the 10-minute mark = **10-minute cooldown** before joining a new session. Abandoning between 10â€“20 minutes = 5-minute cooldown. Abandoning after 20 minutes = no cooldown (player has contributed meaningfully to the run). This creates proportional deterrence rather than a fixed penalty.
- **Random matchmaking grief escalation:** Two successful vote-kicks within 24 hours = 2-hour restriction from public lobbies. Private code-based sessions unaffected.

---

## 14. Progression & Replayability

### 14.1 Run Variation Sources âš‘

- 4 word note locations randomised from 12 possible positions each run
- Word sequence randomised per run
- Final Broadcast monologue randomised per run (weighted pool â€” 75% dramatic, 25% absurdist)
- Item locations randomised from 8 possible positions per floor
- Listener patrol route order randomised within zone constraints
- **Post-launch:** Adaptive system means the Listener plays differently based on group behaviour

**Early Access variation note (v1.3):** In Early Access, the Adaptive system is not active. Run-to-run variation comes from note placement, word sequence, monologue draw, and item locations â€” these are sufficient for the Early Access content volume and keep runs feeling meaningfully different without requiring the full Adaptive system.

### 14.2 Post-Run Stats Screen âš‘

Displayed after every run regardless of win or loss. Stats are shown per player (individual) and per group (collective records). No ranked leaderboard exists â€” no player is highlighted above others for individual performance.

**Stats available in Early Access MVP:**
- Total words spoken per player (displayed as a neutral stat card per player) â€” any continuous audio event above Tier 1 threshold lasting more than 0.3 seconds, separated from the previous event by at least 0.5 seconds of silence, counts as one utterance, plus discrete voice-trigger registrations (Phase 1 note reads, Phase 2 sequence words).
- Longest silence streak (group record)
- Number of Token transfers
- Number of Vocal Sacrifices used
- Listener state at time of each death

> **Speaking time vs. words spoken:** Speaking duration (in seconds) is tracked separately in the local save for Listener's Favourite achievement purposes and is not shown on the stats screen. "Total words spoken" and "total speaking time" are two distinct metrics â€” a player who reads long Phase 1 notes aloud accumulates high speaking time but may register fewer discrete words than a player who chats constantly. The imprinting system (Â§3.4) uses cumulative seconds; the stats screen displays discrete word count.

> **v1.4 note:** The "most talkative player highlighted" framing from v1.3 has been removed. In a game where speaking is dangerous, a public leaderboard highlighting the loudest player creates a grief vector and misaligns the social framing. Stats are shown neutrally per player. The "Listener's Favourite" achievement (Â§15.1) provides a separate shame-acknowledgement mechanic for players who want it, but the default stats screen is neutral.

**Stats added post-launch (hidden until features ship):**
- Number of times the Playback trap was triggered *(unlocks with Playback system)*
- Times ghost bursts were used *(unlocks with ghost burst spectator)*

These fields are not present on the stats screen until the corresponding feature ships â€” not greyed-out or locked, simply absent.

### 14.3 Cosmetic Unlocks

Cosmetics are unlocked through play, not purchased.

- **Token skins** â€” unlocked by completing Steam Achievements
- **Listener skins** â€” unlocked by run milestones
- **Player character skins** â€” unlocked by role-specific milestones
- **Death card backgrounds** â€” unlocked by death-count milestones

**Early Access cosmetic state:** During Early Access, no cosmetics are unlockable â€” the cosmetic unlock state in the local save is initialized as empty. **Achievements can be earned during Early Access but cosmetic rewards are queued for delivery on the first post-launch update.** Steam Achievement badges unlock normally.

Post-launch (Year 1): small paid DLC cosmetic packs may be introduced after 500+ reviews and a positive review trajectory. No paid cosmetics at Early Access launch.

All cosmetic â€” no gameplay impact.

### 14.4 Planned Difficulty Modes
- **Whisper Only:** Normal speech tier reclassified as Loud (Tier 3). Only true whispers are safe. **The Loud role interaction:** The Loud role is **unavailable in this mode** â€” on the role selection screen, The Loud's slot is greyed out with the tooltip: *"Unavailable in Whisper Only â€” this role's minimum Tier 2 speech reclassifies as Tier 3 in this mode, triggering Frenzy every time they speak."* This prevents the permanent-Frenzy loop that would otherwise occur. **The Static bubble interaction:** In Whisper Only mode, speech inside a Static bubble is suppressed entirely â€” the bubble blocks both Listener detection and the Tier 3 environmental side effects (screen flash, physics shatter) for sounds that occur within it. Speech above whisper tier inside the bubble does not affect players outside it.
- **Deaf Run:** Listener audio cues removed. No hum, no clicks, no breath. Navigation by sight and deduction only. **Proximity pulse activation distance is reduced to 6 metres in this mode** to create genuine information loss compared to standard play. **Accessibility note:** The proximity pulse (Â§11.3) remains active in this mode as a visual accessibility alternative, but with the reduced threshold it functions as the primary Listener detection method.
- **One Life:** No ghost mode. Dead players spectate with no interaction ability.

### 14.5 Player Profile & Local Save

A local save file (not cloud-synced at launch) stores:

- Total runs played, total wins, total deaths
- Death tip suppression counter (permanently suppressed after 5 deaths)
- Token-hold run count (experience threshold for tip suppression)
- **Listener's Favourite achievement tracking:** consecutive-run highest-speaker counter (resets to 0 when player is not highest speaker in a run; increments when player is highest speaker; achievement awards at 5 consecutive)
- **Per-run speaking time record** (for Listener's Favourite achievement comparison; not displayed on stats screen)
- Cosmetic unlock state (empty during Early Access)

**Run definition (for tracking purposes):** A 'run' is defined as any session that reaches at least the 5-minute mark. Sessions abandoned before 5 minutes do not increment run counters, death counters, or achievement trackers. Sessions that end via total party wipe, single-survivor fail state, or successful escape all count as completed runs regardless of outcome. The 5-minute threshold aligns with the session abandonment cooldown (Â§13.6) â€” any session long enough to avoid the cooldown counts as a run.

**Living player / surviving player definition:** 'Living player,' 'surviving player,' and 'remaining player' are synonymous in this document â€” all refer to players who have not been caught by the Listener in the current run. Dead players are those who have been caught and entered spectator mode. No revive mechanic exists. If a revive mechanic is introduced post-launch, all instances of these terms must be audited for intended meaning.

**Additional local save data:**
- Calibration data (mic baseline)
- Random matchmaking preferences (region, role preferences)
- Text Broadcaster mode preference
- First-time setup completion flag
- Privacy notice acknowledgement state

**Cloud save** is a post-launch goal (Year 1). At Early Access launch, uninstalling loses local progress. Disclosed in settings menu with a "backup save data" export button.

---

## 15. Steam Achievements & Viral Hooks

### 15.1 Achievements

| Achievement | Condition | Designed Viral Moment |
|---|---|---|
| **Golden Silence** | Escape without any player speaking above Tier 1 (Whisper) except for mandatory Phase 2 word-speaking and Phase 3 Final Broadcast. **Archivist exclusion:** No player may use the Archivist's passive silent registration during this run â€” the achievement is about voice discipline, and the Archivist bypasses Phase 1 risk entirely. **Archivist check timing:** The Archivist exclusion is evaluated at run start (role lock). If any player has the Archivist role selected when the run begins, Golden Silence is ineligible for that run. Post-launch only â€” this exclusion clause is dormant during Early Access. | Requires whisper-only Phase 1 registration under active Token transfer risk, plus silent Phase 2/3 exceptions. The hardest achievement in the game. |
| **No Screaming** | Escape without any player triggering a Tier 3 (Scream) event. "No Screaming" cannot be earned in Whisper Only mode as Phase 2 requires sustained Tier 2 speech (reclassified as Tier 3 in this mode). Achievement attempts must be made in Standard difficulty. | Requires group restraint under pressure. A common but rewarding milestone. (MVP) |
| **Last Words** | Complete the Final Broadcast while holding the Token | High-risk broadcaster moment â€” clip-worthy |
| **Gaslight** | Cause a teammate's death via the Playback trap | Guilt and laughter simultaneously â€” post-launch |
| **Final Broadcast** | Complete the monologue without pause on the first attempt | Skilled execution under maximum pressure |
| **Hot Potato** | Transfer the Token **20 times** in a single run. This requires active passing strategy and deliberate communication beyond normal gameplay flow. **Note:** Token transfers during an active Vocal Sacrifice lock do not count toward Hot Potato.** | Emergent chaos achievement â€” requires coordination |
| **The Sacrifice** | Use Vocal Sacrifice and survive the 30-second window. **Note:** Vocal Sacrifice creates a forced Listener lock, not Frenzy, but still disqualifies the "Heard Nothing" achievement for that run because it is an explicit Listener-targeting override. These two achievements cannot be earned simultaneously. | Heroic moments should be commemorated |
| **Listener's Favourite** | Be the most-spoken player in 5 consecutive runs. **Tracking:** The game records each player's total speaking time per run in the local save file for achievement purposes only. This data is not displayed on the post-run stats screen and is not visible to other players. At the end of each run, the game checks if the local player had the highest speaking time in that session â€” if yes, a counter increments. Five consecutive qualifying runs awards the achievement. The counter resets to 0 if the player is not the highest speaker in any run. **In 2-player sessions one player is always the highest speaker by definition â€” Listener's Favourite is farmable in this session size. This is accepted; the achievement is a shame mechanic, not a skill one.** | Shame achievement â€” the chatty friend receives this |
| **Heard Nothing** | Complete a full run without the Listener entering Frenzy **before Phase 3 begins** and without using Vocal Sacrifice. Phase 3 Frenzy is exempt from the condition â€” the Final Broadcast inherently triggers it. **Any Tier 2+ speech while the Listener is in Hunting state also triggers Frenzy and disqualifies the run.** Groups attempting this achievement must prevent the Listener from entering Hunting state entirely, or ensure no player speaks above Tier 1 while it is Hunting. Runs attempting Heard Nothing must avoid all pre-Phase 3 Frenzy triggers: no screams, no Vocal Sacrifice, no Tier 2+ speech during Hunting. | Requires extraordinary group control |
| **Strangers in the Dark** | Escape with a random matchmaking session where all players are non-friends, regardless of session size. **Friends check timing:** The non-friends check runs at run start and is cached â€” Steam friend status changes during the run do not affect eligibility. **Clarification:** Players who exclusively play with their purchasing group can unlock this achievement by creating or joining a public lobby independently (without their friend group). Quick Join automatically places solo players into random lobbies. The achievement is not permanently blocked for friend-group purchasers â€” it requires a deliberate solo queue session. | Rewards solo queue engagement â€” shareable moment |

**Gaslight** is marked post-launch â€” requires the Playback system to ship first. The Echo role dependency is optional: the achievement triggers when a teammate dies because they responded to a Playback trap, regardless of whether the Echo role was used to set up the scenario. The Listener's native Playback system is sufficient for the achievement condition. **Gaslight detection logic:** The achievement triggers when all three conditions are met within a single Playback event â€” (1) Playback audio plays, (2) a player produces a Tier 1+ voice event within 5 seconds of Playback completing, (3) that player is killed by the Listener within 30 seconds of their response. The 30-second window is used because the Listener's sprint to a Tier 2+ response source takes variable time depending on distance. If the responding player survives past 30 seconds, the Gaslight condition resets. This logic must be implemented as part of the Playback system post-launch.

### 15.2 Steam Page Strategy
- GIF of the Token transferring mid-conversation on the store page
- Video trailer opens with 10-second clip of total silence followed by one player accidentally screaming
- Store page copy: "The game that will ruin your friendship (temporarily)"
- Accessibility features listed explicitly on the store page (proximity pulse, subtitles, Text Broadcaster mode, keybind remapping, Text Broadcaster Phase 2 limitation)
- **Random matchmaking** mentioned on store page: "No group required â€” brave the estate with strangers."
- **Privacy notice** on store page: voice clips are recorded locally in-session for gameplay purposes only and are never transmitted or stored

---

## 16. Solo Dev Scope & Priorities

### 16.1 MVP (Minimum Viable Product) for Early Access âš‘

**Must ship:**
- Single map (Ashford Estate, all 4 floors)
- Core voice mechanic (Token, 4 tiers with calibrated baseline: Tier 0 sub-whisper, Tier 1 whisper, Tier 2 normal, Tier 3 scream)
- Token initial state defined: no Token until first speech; Phase 3 no-Token fallback targets radio location
- Mic calibration screen (mandatory pre-run)
- First-time setup screen (Â§11.4): region selection, role preference, mic calibration, accessibility options, keybind review, privacy acknowledgement
- Lobby screen with role selection, mic check UI, host identifier, Text Broadcaster mode indicator
- Orientation mode (3 minutes 30 seconds â€” see full content spec below)
- The Listener (4 states: Idle/Alerted/Hunting/Frenzy â€” no Adaptive system, with proximity/vision revision plan in Â§4.2.1)
- Tier 0 sub-whisper environmental sound detection (4-metre radius, Alerted only)
- Alerted state dual-timer logic (8-second reset before minute 10 / 15-second Hunting transition after)
- Phase 1, Phase 2 (with session-size-aware player count scaling and fail state), and Phase 3 objectives
- Phase 2 single-survivor fail state with session-size-aware trigger and UI messaging
- Phase 2 total party wipe screen ("The estate is silent. No voice remains.")
- Phase 3 broadcaster death/handoff rules, radio pickup interaction (with grab race condition as documented), teammate Vocal Sacrifice clarification, Text Broadcaster mode
- Phase 3 Listener targeting: Token holder if Token exists; radio location if no Token
- Clock Tower barricadeable staircase door (Phase 2 evasion)
- Silence Room capacity limit (2 players max), Aggressive Check behaviour, and 2-player forced-exit rule
- 2-player mode with adjusted Phase 3 parameters (defender buys time through barricades, movement, and timing; Vocal Sacrifice does not redirect Phase 3 Permanent Frenzy)
- 2â€“4 player multiplayer via Godot Multiplayer + Godot Voice/WebRTC
- 3 MVP roles: Mute (with Silent Drop, 5 bonus gestures, and Floor 4 board absence noted), Loud (F key), Static
- Vocal Sacrifice (G key, grief detection triggers on lock activation not pre-signal)
- Dead player map marking (J key â€” spectate view only, no ghost bursts; phase-around Listener collision)
- Signal Jammer with creak-silencing primary function
- Clap mechanic (Q key, Lights Out only, Tier 0 classification) with HUD cooldown indicator
- Radial gesture wheel (MMB hold, Â§3.7) with hold-compatibility warning in remapping screen
- Full keybind remapping via Settings > Controls (J reserved for dead players only)
- Grief protection (vote-kick, session cooldown, Vocal Sacrifice lock-based grief detection, random matchmaking grief escalation)
- Disconnect handling with 60-second rejoin window
- Accessibility HUD elements (proximity pulse, direction indicator, subtitles â€” Playback warning deferred)
- Text Broadcaster mode (Phase 3 keyboard input, Phase 2 limitation disclosed)
- Audio settings screen (Playback warning toggle greyed out)
- Death card tip system (first 5 deaths, including radio grab tip)
- Post-run stats screen (MVP stats only â€” neutral per-player display, no leaderboard; Playback and ghost burst stats hidden)
- Local save (all profile data, calibration, cosmetic unlocks, matchmaking and accessibility preferences, setup completion flag, privacy acknowledgement)
- Voice data privacy notice on first launch (First-Time Setup) and lobby screen
- Second Listener dead-target fallback rule (falls back to Token holder if primary target is dead)
- Dead player imprint decay rule (60 seconds post-death)
- Adaptive metric tie-breaking rule (Metric A priority) â€” dormant in MVP but rule must be implemented for post-launch activation
- Random matchmaking system (Â§18) â€” public lobby browser, region selection, role preference opt-in

**Cut for post-launch:**
- Adaptive Listener evolution
- Playback trap / voice recording / Playback warning HUD indicator
- Echo, Archivist, and Witness roles
- Ghost burst spectator system
- Ghost burst Phase 2 dead-player word contribution
- Host migration
- Cloud save
- Gaslight achievement (requires Playback system)

**Orientation mode â€” full content spec (v1.3 + v1.4 + v1.8 additions):**
The **3-minute-30-second** orientation activates automatically for first-time players and optionally for returning players. The Listener is present but passive (no targeting, walks at 0.5Ã— speed, ignores players). During the passive phase (0:00â€“3:15), a brief text overlay appears: *"The Listener is dormant. It will activate at the end of orientation."* This prevents confusion about whether the passive phase represents normal gameplay. Players can move and interact. The orientation covers 7 segments:

1. **(0:00â€“0:20) Token introduction:** A skull icon appears above a dummy NPC that "speaks" â€” players see the Token appear and transfer. Text overlay: *"Whoever speaks last is the Listener's target."*
2. **(0:20â€“1:00) Voice tiers:** Players are prompted to speak at different volumes and see the microphone HUD indicator change colour. The Listener's eye sockets respond visually.
3. **(1:00â€“2:00) Gesture vocabulary:** All 8 standard gestures demonstrated via prompted key presses with on-screen labels. The radial gesture wheel is also demonstrated: players are prompted to hold MMB and drag to select a gesture. **Extended to 60 seconds** to account for 9 inputs (8 gestures + radial wheel) at ~3 seconds per input plus label reading. **Mute bonus gestures note:** *"If you play as The Mute, 5 additional gestures become available (keys 1â€“5). See the gesture reference card in the ESC menu."* This note appears regardless of selected role, allowing players to learn about the Mute's bonus gestures without pre-selecting it.
4. **(2:00â€“2:20) Vocal Sacrifice:** A brief demonstration of the G-hold pre-signal amber pulse. Players do not need to complete the sacrifice â€” they just see the pre-signal animation.
5. **(2:20â€“2:45) Clap mechanic:** At a simulated Lights Out, players are prompted to press Q and see the echolocation flash. The HUD cooldown indicator is shown. **Extended to 25 seconds** to account for input plus animation observation.
6. **(2:45â€“3:15) Phase objectives overview:** Text overlay showing Phase 1 (find notes), Phase 2 (speak sequence at tower, requires more than one survivor), Phase 3 (broadcast â€” a teammate creates the window, not the broadcaster). No hands-on interaction â€” information only.
7. **(3:15â€“3:30) The Listener activates & Ghost tutorial:** The Listener's eyes glow red and it begins Hunting at **1.3Ã— speed**. **At the 3:15 mark, the orientation script force-assigns the Token to the player with the lowest run count (or a random player if all are equal).** A brief text overlay fires: *"The Listener has a target â€” [PlayerName]."* This teaches the Token indicator's function (the skull appears above that player) without extra UI. This guarantees the Hunting segment demonstrates actual pursuit, not confused wandering. **Token transfer is suppressed during the orientation Hunting segment** â€” the force-assigned Token cannot be transferred by speech during this window. This ensures the demonstration shows a real pursuit without being disrupted by incidental speech. Players are prompted to survive. This is a **live-consequence demonstration** (scripted response to death). If a player dies during this segment, they are immediately shown the **Spectator HUD** and prompted to press `J` to place a map marker for surviving teammates. This introduces the dead player's agency before the first real run. Orientation ends after the 15-second window regardless of how many players survive or if a total party wipe occurs.

**Single-player orientation exception:** In single-player orientation (only 1 player present), the ghost tutorial segment (3:15â€“3:30) is replaced by a text-only summary: *"If you die in a run, press J to place markers for your teammates. You can still see the Listener's position and guide your team."* The Listener does not activate in single-player orientation â€” the live-consequence demonstration requires at least 2 players to be meaningful.

### 16.2 Build Order

**Week 1â€“2: Voice Stack Validation**
Prototype mic input â†’ amplitude reading â†’ tier classification (all 4 tiers including Tier 0) â†’ Token transfer. Test with two real players over a real network. Validate Godot Voice/WebRTC amplitude API consistency and latency. Test audio mix levels. Confirm no Token exists until first speech, and Phase 3 radio-location fallback functions correctly. If Godot Voice/WebRTC does not work after 2 weeks, evaluate SDK alternatives. Nothing else gets built until the core mechanic is confirmed viable.

**Month 1â€“2: Core Loop**
Build mic calibration screen and First-Time Setup screen (Â§11.4). Build lobby screen (role selection, mic check, host label, Text Broadcaster toggle, privacy notice). Build Token system with initial-state logic and Phase 3 no-Token fallback. Implement Vocal Sacrifice pre-signal and countdown HUD (grief detection on lock activation). Implement J-key dead player map marking with Listener phase-around collision. Implement radial gesture wheel (MMB). Build matchmaking backend skeleton.

**Month 3â€“4: The Listener**
Build estate blockout (grey box). Implement Listener NavMesh AI with all 4 states including dual-timer Alerted logic, Tier 0 detection, dead-target fallback for second Listener, imprint decay on death, and the proximity/vision revision plan in Â§4.2.1. Test Listener response to Token holder, scream-first chase override, 5-metre attack kills, stationary-player vision safety, and Phase 3 radio-location targeting. Tune detection radii through playtesting. Prototype Playback trap separately in isolation to validate feasibility â€” if not stable by end of Month 4, cut it from post-launch scope. **Playback contingency note:** If Playback is cut post-Month 4 feasibility check, the following must be revised: Â§10.1 (Earmuffs removed from item pool), Â§15.1 (Gaslight achievement removed), Â§13.3 (privacy notice simplified â€” no voice recording), Â§14.2 (Playback stat field removed entirely, not just hidden), Â§18.4 (random lobby Playback danger note removed). Maintain a 'Playback-dependent features' checklist updated at each design revision.

**Month 5â€“6: Objectives and Map**
Implement Phase 1 (note pickup, voice registration, Mute Silent Drop, Registration Boards on Floors 1â€“3 only). Implement Silence Room with 2-player cap, Aggressive Check, and 2-player forced-exit rule. Implement Phase 2 with session-size-aware player count scaling, single-survivor fail state, total party wipe screen, and Text Broadcaster Phase 2 limitation. Implement Phase 3 with handoff rules, Text Broadcaster mode, radio pickup interaction, Phase 3 Vocal Sacrifice disablement during active broadcast, and Clock Tower barricadeable door. Add win/loss conditions. Connect matchmaking system to lobby flow.

**Month 7â€“8: Art and Sound**
Validate art direction in Month 7 Week 1 with actual lighting and shaders before full art pass. Replace grey box with final low poly manor art. Add Listener character model. Implement all audio including Tier 0 environmental sound events. Add HUD including Clap cooldown indicator, initial-state Token logic, and session-size-aware fail state display.

**Month 9â€“10: Polish and Roles**
Add 3 MVP roles with correct keybinds and death notifications. Implement keybind remapping system (J reserved, hold-action compatibility warning). Implement Signal Jammer creak-silencing. Implement accessibility HUD elements. Implement audio settings screen. Implement death card tip system (including radio grab tip). Grief protection systems. Basic escalation (Lights Out, second Listener with dead-target fallback). **QA debug mode for Lights Out testing:** Implement a debug mode that forces Lights Out at run start for playtesting purposes. The Clap mechanic, torch battery system, and Lights Out escalation require dedicated playtesting sessions at the 25-minute state â€” standard runs may not reach this consistently. Debug mode is removed before Steam submission. Implement full orientation mode (with radial wheel demo and Hunting-speed clarification). First external playtesting. Steam page setup.

**Month 11â€“12: Early Access Prep**
Fix playtesting bugs. If Playback prototype was stable: integrate and test. If not: confirm scope cut and remove all references. Finalise lobby, rejoin flow, and matchmaking browser. Add privacy notice flow and First-Time Setup screen. Confirm GDPR compliance for EU launch. Submit to Steam. Launch in Early Access.

### 16.3 Visual Tone Guidance

The art direction is "low poly, PS1-era aesthetic, heavy shadows." KayKit and Kenney asset packs are the planned sources:

- **KayKit** is bright and gamified by default. Requires dark palette override, custom shadowing, and post-processing (desaturation, vignette) to read as horror.
- **Kenney** is flat and minimal. May feel too casual for gothic horror.

Before committing to either for the full art pass (Month 7), prototype the visual tone in Month 3 during the grey box phase. Test question: does the manor feel threatening with these assets and this lighting? If no, evaluate KayKit's Graveyard packs or Synty Horror Environment.

### 16.4 Tech Stack Summary and Budget Estimate âš‘

| System | Tool | Free Tier Limit | Estimated Cost at Scale |
|---|---|---|---|
| Engine | Godot 4.x (.NET/C#) | Free (under $200k revenue threshold) | $0 at launch. Revenue share kicks in post $200k â€” estimated at ~2,200 units sold |
| Networking | Godot Multiplayer API | Free | $0 |
| Relay | Steamworks.NET / Godot Steam Relay | Free up to 50 CCU | ~$0.49/GB data relay. Estimated ~5â€“10MB per session at 4 players. **At 50 CCU:** free. **At 500 CCU:** ~$6â€“12/month. **At 5,000 CCU:** ~$60â€“120/month |
| Lobby | Steam Lobby | Free up to **250 concurrent lobbies** | **Risk zone:** 250 lobbies â‰ˆ 1,000 concurrent players. Beyond 250: ~$0.005/lobby-hour. **At 500 lobbies:** ~$50â€“150/month. **At 2,000 lobbies:** ~$200â€“600/month |
| Voice Chat (primary) | Godot Voice/WebRTC | Free for indie (under certain MAU thresholds) | $0 at launch. Re-evaluate at 10,000+ MAU |
| Voice Chat (fallback) | Dissonance or Photon Voice | N/A | $50â€“$200 one-time (Dissonance) or usage-based (Photon) |
| Voice Recording | Godot AudioStreamMicrophone | Built-in | $0 |
| Character Assets | KayKit | Free with credit | $0 |
| Environment Assets | Kenney | Free (CC0) | $0 |
| Version Control | Git + GitHub | Free (private repo) | $0 |
| Steam | Steamworks | $100 one-time app fee | **$100 upfront** â€” the only confirmed required spend |
| Legal / Privacy review | External counsel (GDPR, EULA) | N/A | **Estimated $300â€“800** for a single contract/privacy review session with an indie-focused games lawyer. Strongly recommended before Playback ships post-launch. Optional at Early Access launch if voice recording is inactive. |

**Total confirmed upfront cost: $100 (Steam fee)**

**Total estimated monthly infrastructure cost at Early Access launch (low player count):** $0â€“$15/month

**Steam Lobby cap contingency plan (v1.3):** The free tier covers 250 concurrent lobbies â€” approximately 1,000 concurrent players. Contingency:

1. Monitor Steamworks Dashboard lobby concurrency daily in the first two weeks post-launch
2. Allocate first $200â€“500 of Steam revenue exclusively to infrastructure overage before any other spending
3. Steamworks (Free) activate automatically once the free limit is exceeded â€” a credit card must be on file with Steamworks.NET before launch
4. Worst-case monthly estimate at 5,000 concurrent players: ~$500â€“1,500/month. At this player count, revenue comfortably covers this
5. Fallback: if costs become unmanageable before revenue catches up, implement a short-term session cap (max 300 public lobbies) with a lobby-full queue notification

---

## 17. Business Model & Pricing

### 17.1 Early Access Pricing

**Target price: $9.99 USD**

Lower prices enable "gifted to friends" purchasing behaviour â€” the primary viral growth driver for co-op indie games. Lethal Company's $9.99 launch price directly enabled group purchases and contributed to its rapid word-of-mouth spread.

Offer a **4-pack bundle at 12.5% discount relative to current unit price**. Bundle prices are rounded up to the nearest $0.49 price point for Steam storefront aesthetics. At $9.99 per unit, this equals $34.99 (4 Ã— $9.99 = $39.96, 12.5% discount = $34.97, rounded to $34.99). If the Early Access price increases, the bundle price adjusts to maintain the 12.5% discount with the same rounding rule (e.g. at $11.99/unit, 4-pack = $41.99; at $12.99/unit, 4-pack = $45.49). This ensures the bundle discount remains consistent regardless of base price changes.

**Early Access price ceiling:** Do not exceed $12.99 during Early Access. The current target of $9.99 leaves room for a potential increase to $11.99 or $12.99 during Early Access if scope expands significantly.

### 17.2 Post-Launch Revenue

- **1.0 Release:** Price may increase to **$12.99** upon full launch exit from Early Access â€” a $2â€“3 increase from the Early Access base price, timed with the second map release. This is a genuine price increase, not notional.
- **Cosmetic DLC packs** (Year 1 post-launch): $2.99â€“$4.99 after 500+ reviews and a positive review trajectory. Never at Early Access launch.
- **Second map (Year 1):** Free update included in base game price. Should not be paywalled.

### 17.3 Player Retention Roadmap

For a 30-minute session game with one map, the realistic engagement ceiling before retention drops is 8â€“15 hours for casual players, longer for streamers and achievement hunters.

**Second map design should begin in Month 9â€“10** alongside polish work. Second map design direction:
- Different architectural setting (exterior grounds, greenhouse, or chapel)
- Different primary floor mechanic (weather-based sound masking, different Listener variant behaviour)
- Same three-phase objective structure with new word pool and new monologue pool
- New unique item pool

### 17.4 Infrastructure Cost Projections âš‘

See Â§16.4 for full infrastructure cost table. Summary for business planning:

| Scenario | Estimated Monthly Infrastructure | Revenue Required to Break Even |
|---|---|---|
| Soft launch (under 250 CCU lobbies) | $0â€“$15 | $0 (free tier) |
| Moderate success (500 CCU lobbies) | $50â€“$200 | ~20â€“25 units sold/month |
| Strong indie performance (2,000 CCU lobbies) | $200â€“$700 | ~70â€“100 units sold/month |
| Viral event (5,000+ CCU lobbies) | $500â€“$1,500/month | ~150â€“200 units sold/month |

At $9.99 with Steam's 30% cut, net per unit is approximately $6.99. Infrastructure costs at viral scale are covered by a modest ongoing sales volume.

---

## 18. Random Matchmaking System

### 18.1 Design Intent

Random matchmaking solves a core co-op problem: the intended public co-op experience targets 2-4 players, but not every interested player has a pre-formed group. The system must be simple enough to implement within the solo dev timeline and robust enough that strangers can complete a run together without prior coordination. Private custom rooms are allowed to start with 1 player for testing, practice, content creation, and self-imposed challenge runs; solo custom play is an intentional exception, not a lobby validation bug.

The matchmaking feature is listed as an MVP requirement (Â§16.1) because its absence limits the addressable audience at launch.

### 18.2 Public Lobby System

**Lobby browser:** Players can create or join public lobbies from the main menu. Each entry displays:

| Field | Display |
|---|---|
| Host name | Steam display name |
| Current players | e.g. "2 / 4" |
| Region | e.g. "EU West", "NA East", "SEA" |
| Status | "In Lobby" or "In Run" |
| Role slots open | Brief indicator of unclaimed roles |
| Text Broadcaster | Icon if any player has Text Broadcaster mode active |

**Role slot visibility in the lobby browser is intentional pre-run matchmaking information. Once the run begins, roles are not displayed to teammates, consistent with Â§5.**

Players in an active run are visible as "In Run" but cannot be joined mid-session.

**Quick join:** A single button matches the player to the most populated available lobby in their preferred region. If no lobby exists in their region, it expands to adjacent regions. If no lobby exists at all, the player becomes host and a new public lobby is created. **Region adjacency for Quick Join fallback:** NA East â†’ NA West â†’ EU West â†’ EU Central (and reverse). SEA â†’ OCE â†’ NA West (and reverse). No direct adjacency between EU and SEA/OCE. If no lobby exists in any adjacent region, Quick Join creates a new public lobby in the player's home region rather than expanding to non-adjacent regions. Cross-region private sessions remain possible via session code regardless of adjacency.

### 18.3 Session Structure for Random Lobbies

Random lobbies follow the same session structure as private lobbies with the following adjustments:

**Forced orientation mode:** If any player in the lobby has fewer than 3 prior runs logged, orientation mode activates automatically. Experienced players cannot skip on behalf of new players.

**Text chat in lobby:** A lobby text chat window is available before the run begins. Closes when the run starts.

**Pre-run mic check display:** The lobby screen's mic level meters are visible to all players simultaneously.

**Consent reminder:** Before the first run in a random lobby: *"In-game voice chat is required. Speaking transfers the monster's attention to you."*

**Voice data privacy notice:** The lobby screen also displays: *"Voice is recorded locally this session for gameplay purposes only. Nothing is transmitted externally."* This notice is shown in every random lobby, not just the first.

### 18.4 Strangers and Communication Trust

**Vocal Sacrifice pre-signal** ensures strangers can recognise a sacrifice attempt without verbal pre-communication.

**The radial gesture wheel (Â§3.7) is particularly valuable in random lobbies**, where players may not have memorised the 8-key layout before playing together. The wheel surfaces all gestures visually without requiring prior memorisation.

**The Token mechanic is self-teaching regardless of group composition.** Players learn within 30 seconds that speaking passes the Token.

**Distrust is a feature, not a problem.** Strangers are more susceptible to the Playback trap â€” a random lobby generates emergent tension that a friend group rarely experiences with the same intensity.

### 18.5 Region and Server Selection

Players select their preferred region during the First-Time Setup screen (Â§11.4). Regions available at launch:

- NA East
- NA West
- EU West
- EU Central
- SEA (Southeast Asia)
- OCE (Oceania)

Region selection determines which Steamworks.NET Relay server is used. Cross-region sessions are possible for private code lobbies. The browser filters to preferred region by default.

**Latency display:** The lobby browser shows estimated ping to each listed lobby. Lobbies with ping > 150ms are flagged with a warning icon. Players joining a high-ping lobby receive a one-time acknowledgement prompt.

### 18.6 Anti-Grief Measures for Public Lobbies

In addition to standard grief protection (Â§13.6):

- **Random matchmaking grief escalation:** Two successful vote-kicks within 24 hours = 2-hour public lobby restriction
- **Lobby lock:** Host can lock the lobby before the run starts
- **Mute player (audio only):** Any player can locally mute another player's VC from the scoreboard overlay. Does not affect gameplay â€” Token and Listener tracking remain active
- **Report system:** Post-run in-game report form. Reports batched and reviewed manually

### 18.7 Solo Queue Achievement

The **Strangers in the Dark** achievement rewards players who complete a full escape run in a random matchmaking session where no players are Steam friends â€” regardless of session size. In a 2-player run, this means both players must be strangers. Designed to incentivise solo queue engagement and generate a shareable moment.

---

### Appendix A: Playback-Dependent Features Checklist

If the Playback trap system (Â§3.5, Â§4.4) is cut at Month 4 feasibility review, the following sections must be revised or removed:

- **Â§3.5 Playback Trap** â€” Remove all Playback-related content, including 90-second silence triggers, undistorted mode, and random matchmaking timing notes
- **Â§4.4 Voice Mimicry** â€” Remove entirely; dependent on Playback recordings
- **Â§10.1 Earmuffs item** â€” Remove from item pool table; item is Playback-dependent
- **Â§13.3 Voice data privacy notice** â€” Simplify to remove voice recording references (Early Access) or activate with Playback (post-launch)
- **Â§14.2 Post-run stats screen** â€” Remove "Playback trap triggered" stat field entirely (not just hidden)
- **Â§15.1 Gaslight achievement** â€” Remove; requires Playback system to function

---

## Content

### Included in Early Access MVP

- **One playable manor floor** — the Ashford Estate Basement, used to validate the core loop.
- **Voice tier system** — Silent / Whisper / Normal / Shout classification based on calibrated baseline.
- **The Listener AI** — Idle, Alerted, Hunting, and Frenzy states with vision, hearing, and navigation.
- **Last Word Token** — transfers to whoever speaks at Tier 1+, directing the Listener's primary target.
- **Three-phase objective system** — Find the Words, Speak the Sequence, The Final Broadcast.
- **Four base player roles** — Mute, Loud, Echo, Static (Echo deferred to post-launch).
- **HUD** — Token visual, phase tracker, death card, and accessibility Text Broadcaster mode.
- **Multiplayer** — ENet listen-server with direct-IP room codes; Steam deferred.

### Post-Launch Content

- Adaptive Evolution system, second Listener, full Playback Trap, additional roles (Archivist, Witness), all four floors fully arted, and Steam integration.

## Acceptance Criteria

1. A reader can identify the core fantasy, unique selling point, and intended emotional arc from the Overview and Player Fantasy sections.
2. The Core Loop section describes a complete `[start → challenge → resolution]` cycle that matches the three-phase objective system.
3. Every mechanic in §3–§11 is documented with unambiguous rules, thresholds, and edge cases.
4. All formulas (tier percentages, Listener speeds, detection radii, phase timers) are stated explicitly and consistently.
5. Player roles, items, and environmental interactions each have clear rules and interactions with other systems.
6. Death, spectator, and fail-state conditions are specified for all session sizes (2, 3, and 4 players).
7. Multiplayer architecture and Steam-deferred scope are documented.
8. Progression, achievements, and post-launch roadmap are present.
9. Tone is consistent with Pillar 6: frightening systems, occasionally absurd content.
10. All internal cross-references between sections resolve to documented mechanics.

