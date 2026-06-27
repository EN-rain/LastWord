# Step 1 Audit — Game Concept Document

## Created
- `design/gdd/game-concept.md`

## Verification
- All 8 required sections present: Overview, Player Fantasy, Detailed Rules,
  Formulas, Edge Cases, Dependencies, Tuning Knobs, Acceptance Criteria.
- Verify command exited 0.

## Source Material
- CCGS template: `claude-code-game-studios/.claude/docs/templates/game-concept.md`
- GAME_DESIGN.md sections read: 1 (Game Overview), 2 (Core Design Philosophy / 7
  Pillars), 3 (Voice Mechanics), 7 (Objective System — Three Phases), 9 (Death &
  Spectator System).

## Tone Choices
- Adopted the "Ashford Estate register" requested in GAME_DESIGN.md 7.1/7.3:
  frightening systems, occasionally absurd content.
- Opening blockquote establishes the register immediately.
- Overview is evocative but precise. Player Fantasy names specific emotions
  (tension, guilt, betrayal, absurdity) drawn from the One-Line Pitch, High
  Concept, and Pillars 1-7.
- Edge Cases section remains unambiguous — the register does not sacrifice
  clarity for style; it pairs them.
- Acceptance Criteria use checklist format (testable) but avoid dry corporate
  language.

## Deviations from CCGS Template
- The original CCGS template is very large (~20 sections: Elevator Pitch, Core
  Identity, Core Fantasy, Unique Hook, MDA Framework, Player Motivation, Core
  Loop, Game Pillars, Inspiration, Target Player, Technical, Risks, MVP, etc.).
- This document follows the 8-section structure specified in the worker
  instructions, not the full CCGS template. The 8 sections map loosely:
  - Overview = Elevator Pitch + Core Identity + Technical snapshot
  - Player Fantasy = Core Fantasy + Unique Hook + Player Motivation
  - Detailed Rules = Core Loop + Game Pillars (embedded in phase descriptions)
  - Formulas / Edge Cases / Dependencies / Tuning Knobs = Additional
    mechanics detail
  - Acceptance Criteria = New, testable checklist

## Mechanical Accuracy
- Tier thresholds (35%/100%/200%) match GAME_DESIGN.md 3.3.
- Listener speed multipliers (0.3x Idle, 1.0x Hunting, 1.8x Frenzy) and 5m
  attack range match 3.3 and 4.2.
- 3-phase structure matches 7.1-7.3 exactly (note finding, sequence speaking,
  final broadcast).
- Death sequence and spectator rules match 9.1-9.2.
- Escalation timeline markers (10/20/25/30 min) match 8.1.
- Edge cases are verbatim or paraphrased from GAME_DESIGN.md without invention.

## No New Mechanics
- No mechanics were invented that do not appear in GAME_DESIGN.md.
- Post-launch features (Playback Trap, Echo decoy, Archivist silent register,
  ghost burst) are explicitly marked as post-launch where mentioned.

## Word Count
2459 words.
