# Phase 2 — Note System

Implements the four-piece note registration loop for Phase 1 (per `GAME_DESIGN.md` §7.1).

## Files added

- `Scripts/World/NoteItem.cs` — pickup node. Holds an `AssignedWord` + `Tier`,
  defaults to a small yellow `BoxMesh` if no `NoteMesh` is assigned. Emits
  `NotePickedUp(word, tier, peerId)` from `PickUp(PlayerController)` and then
  frees itself. Peer ID is derived from `player.Name` (GameManager sets
  `Name = peerId.ToString()` at spawn), falling back to the multiplayer
  authority if the name is not numeric.

- `Scripts/World/WordRegistry.cs` — authoritative list of registered words
  for the run. Stores normalised strings, is idempotent on duplicates, and
  emits `RegisteredWord` per entry and `AllWordsRegistered` once
  `TotalWords` (default 4) is reached. `Clear()` resets for a new run.

- `Scripts/World/RegistrationBoard.cs` — `Area3D` placed in F1_Basement.
  Subscribes to `VoiceManager.TierChanged` via the singleton instance.
  When the tier rises to Whisper or higher, it reads the current token
  holder from `VoiceManager.Instance.TokenHolderPeerId`, checks the player
  is within the board's detection radius (`OverlapsBody`), looks up the
  held word from `GameManager`, emits `WordRegistrationRequested`, and
  forwards the word to `WordRegistry`. The hold is then cleared so the
  same note cannot be registered twice.

## File modified

- `Scripts/Core/GameManager.cs`:
  - Added `Dictionary<long, (string word, int tier)> _heldNotesByPeer`.
  - Added `SpawnNoteItems()`: finds `F1_Basement/SpawnPoints`, collects
    the 4 `NoteSpawn_*` `Marker3D`s, shuffles them with `new Random()`,
    draws 4 distinct Tier-1 words via `WordPool.DrawDistinct(WordPool.Tier.One, 4, rng)`,
    instantiates `NoteItem` nodes (no `.tscn` required — the script
    creates its own default visual), assigns each a word, parents it
    under `SpawnPoints`, snaps its global transform to the marker, and
    subscribes `OnNotePickedUp` so the held-note dictionary stays current.
  - Added `OnNotePickedUp`, `GetHeldNote(peerId)`, `ClearHeldNote(peerId)`.
  - Calls `SpawnNoteItems()` from `_Ready()` after the F1_Basement load
    block, before the multiplayer setup.

## Deviations from the spec

The spec text contained two snippets that would not compile against the
existing codebase. Both deviations are commented in the code:

1. `RegistrationBoard.OnVoiceTierChanged` — spec wrote
   `OnVoiceTierChanged(int tier, long peerId, string peerName)`, but
   `VoiceManager.TierChanged` only emits `(int newTier)`. The spec
   signature would fail at runtime via the string-based `vm.Connect(...)`
   call because the Callable would not match the signal's argument list.
   Implementation instead subscribes to the actual signal and pulls
   peer info from `VoiceManager.Instance.TokenHolderPeerId`. A
   `CallDeferred` retry covers the case where `VoiceManager.Instance`
   is not ready when the board's `_Ready` runs.

2. `player.HeldNoteWord` / `player.PeerId` — neither exists on
   `PlayerController`. Per the spec's own guidance ("if not, use a
   different approach (e.g., track picked-up notes in GameManager)"),
   held-note state lives on `GameManager` and is keyed by the player's
   peer ID (derived from `player.Name`).

`NoteItem.PickUp` also uses `player.Name`-based peer ID derivation
rather than a non-existent `player.PeerId` property. `WordRegistry.cs`
is implemented verbatim.

## Verification

```
A_PASS  Scripts/World/NoteItem.cs has class NoteItem and NotePickedUp
B_PASS  Scripts/World/RegistrationBoard.cs has class RegistrationBoard and WordRegistrationRequested
C_PASS  Scripts/World/WordRegistry.cs has class WordRegistry and AllWordsRegistered
D_PASS  Scripts/Core/GameManager.cs has SpawnNoteItems and DrawDistinct
```
