# Voice Mechanics Implementation Spec

## Scope

Implement the missing **MVP** Voice Mechanics from `REQUIREMENTS.md` §3:

- [x] 3.1 Token transfer — already done
- [x] 3.3 Tier classification — already done
- [ ] **3.2** Mic calibration flow — wire lobby UI
- [ ] **3.6** Vocal Sacrifice
- [ ] **3.7** Gesture system (8 standard keys)
- [ ] **3.7** Radial gesture wheel (MMB hold)
- [ ] **3.7** Clap ability (Q, post-Lights Out)
- [ ] **3.7** Role ability keys: F (Loud stun), R (Static bubble)

Explicitly **out of scope** (post-launch / P2):

- 3.4 Vocal imprinting
- 3.5 Playback trap
- 3.7 T (Echo replay decoy)

---

## Common patterns

### Input actions

`SettingsMenu` already defines these actions in its `_actionNames` list and `EnsureDefaultActions`. Verify/create them in `project.godot` Input Map:

```text
move_forward, move_backward, move_left, move_right, move_jump, move_sprint
gesture_z, gesture_x, gesture_c, gesture_v, gesture_b, gesture_n, gesture_m, gesture_l
clap_q, sacrifice_g
ability_f, ability_r
radial_wheel_mmb
spectator_j (reserved, locked)
```

### Authority / networking

- Local authority player reads input in `PlayerController._PhysicsProcess` (already gated by `!PauseMenu.IsOpen`).
- For abilities that change Listener state, client sends an `[Rpc(AnyPeer, CallLocal=false)]` request; server validates and either applies directly or calls an `[Rpc(Authority, CallLocal=true)]` sync.
- For noise-based abilities (Clap, Loud stun pulse), use `VoiceManager.Instance.BroadcastNoiseEvent(tier, SoundKind.Special, isSpecialLongRange)`. `VoiceManager` already routes client→server→ListenerAI.
- For suppression abilities (Static bubble), override `ListenerAI.IsAudioSuppressedByFutureSystems(ListenerSoundEvent)`.

### Cooldown pattern

```csharp
private float _cooldownRemaining;
[Export] public float Cooldown = 12f;

public override void _Process(double delta)
{
    if (_cooldownRemaining > 0f)
        _cooldownRemaining -= (float)delta;
}

private bool CanUse() => _cooldownRemaining <= 0f;
private void StartCooldown() => _cooldownRemaining = Cooldown;
```

---

## 1. Mic calibration UI flow

### Current state

- `VoiceManager` has `StartCalibration()`, `ProcessCalibration()`, `CalibrationProgress` and `CalibrationFinished` signals.
- `SettingsMenu` already wires calibration button + progress bar + status label.
- `CustomLobby` and `MatchmakingLobby` display a live mic meter per player but do **not** expose a calibration CTA.

### Required changes

1. Add a **"Calibrate Mic"** button to both lobby scenes (`CustomLobby.tscn`, `MatchmakingLobby.tscn`) with a progress bar.
2. In `CustomLobby.cs` / `MatchmakingLobby.cs`:
   - On button pressed: call `VoiceManager.Instance.StartCalibration()`.
   - Connect `CalibrationProgress` to the progress bar.
   - Connect `CalibrationFinished` to re-enable the button and show baseline value.
   - Disable Ready/Start buttons while `VoiceManager.Instance.IsCalibrating` is true.
3. Save calibrated baseline to `user://settings.cfg` (already done inside `VoiceManager.LoadBaseline` / `SaveBaseline`? Verify; if not, add save in `CalibrationFinished` handler).
4. First-Time Setup screen is out of scope; lobbies are the MVP calibration entry point.

### Acceptance

- [ ] Host and clients can each click Calibrate in lobby.
- [ ] 30-second progress bar fills while speaking.
- [ ] Button re-enables and shows "Baseline: X.XX" when finished.
- [ ] Ready button disabled during calibration.

---

## 2. Gesture system + radial wheel

### Design reference (GAME_DESIGN.md 3.7)

| Key | Gesture |
|---|---|
| Z | Point forward |
| X | Wave / come here |
| C | Thumbs up |
| V | Shake head / no |
| B | Hand to ear |
| N | Hold up note |
| M | Point at self |
| L | Cross arms / stop |

- Gestures visible at 6m, no sound, no Token transfer.
- MMB hold 0.3s opens radial wheel with 8 segments.
- 20px deadzone; release without drag dismisses.

### Files

- New: `Scripts/Player/GestureSystem.cs`
- New: `Scripts/UI/GestureWheel.cs`
- New scene: `Scenes/GestureWheel.tscn` (CanvasLayer + TextureRect/Control)
- Modify: `Scenes/Player.tscn` (add GestureSystem node)
- Modify: `Scenes/HUD.tscn` (add GestureWheel instance)
- Modify: `Scripts/Player/PlayerController.cs` (route gesture input)

### GestureSystem.cs

```csharp
public enum GestureId
{
    PointForward, Wave, ThumbsUp, No, Listen, NoteFound, Self, Stop
}

public partial class GestureSystem : Node3D
{
    [Signal] public delegate void GesturePlayedEventHandler(GestureId gesture);

    [Export] public float MaxVisibilityDistance = 6f;
    [Export] public float GestureDuration = 1.5f;

    private GestureId? _currentGesture;
    private float _gestureTimer;
    private AnimationPlayer _animPlayer;

    public override void _Ready()
    {
        _animPlayer = Owner.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
    }

    public void PlayGesture(GestureId gesture)
    {
        if (_currentGesture == gesture) return;
        _currentGesture = gesture;
        _gestureTimer = GestureDuration;
        EmitSignal(SignalName.GesturePlayed, (int)gesture);
        // TODO: trigger animation clip by name if present
    }

    public override void _Process(double delta)
    {
        if (_gestureTimer > 0f)
        {
            _gestureTimer -= (float)delta;
            if (_gestureTimer <= 0f)
                _currentGesture = null;
        }
    }
}
```

### Networking

- Gesture input is read locally; trigger an RPC to sync to other players.
- Since gestures have no gameplay consequence, use `[Rpc(Authority, CallLocal=false, TransferMode = Unreliable)] private void SyncGesture(int gestureId)` called from client→server→all.
- Server validates sender, then `Rpc(nameof(SyncGesture), gestureId)`.

### GestureWheel.cs

- CanvasLayer/Control, hidden by default.
- Input: read `radial_wheel_mmb` in `_Input` or `_Process`.
- Hold 0.3s → show wheel centered at screen center.
- While shown, sample mouse position relative to center.
- Divide into 8 pie slices (22.5° offset, each 45°).
- Deadzone radius 20 px.
- On release: if outside deadzone, emit `GestureSelected(int)`.

### PlayerController integration

In `_PhysicsProcess`, inside the `!PauseMenu.IsOpen && IsMultiplayerAuthority()` block:

```csharp
if (Input.IsActionJustPressed("gesture_z")) _gestureSystem.PlayGesture(GestureId.PointForward);
else if (Input.IsActionJustPressed("gesture_x")) _gestureSystem.PlayGesture(GestureId.Wave);
// ... etc
```

### Acceptance

- [ ] All 8 keys trigger a gesture locally and replicate to peers.
- [ ] Holding MMB 0.3s opens radial wheel; releasing outside deadzone triggers mapped gesture.
- [ ] Gestures do not transfer Token or emit sound.
- [ ] Visible state replicates (animation trigger + debug label optional).

---

## 3. Clap ability

### Design reference

- Key: `Q`
- Available only after Lights Out (25-min mark).
- Illuminates room geometry for 0.5s.
- Emits Tier 0 noise at 15% baseline, 4m detection radius.
- Cooldown 12s.
- HUD cooldown bar appears only after first use.

### Files

- New: `Scripts/Player/ClapAbility.cs`
- Modify: `Scripts/Player/PlayerController.cs`
- Modify: `Scripts/UI/HUDManager.cs` (cooldown bar)

### ClapAbility.cs

```csharp
public partial class ClapAbility : Node3D
{
    [Export] public float Cooldown = 12f;
    [Export] public float IlluminateDuration = 0.5f;
    [Export] public float SoundRadius = 4f;
    [Export] public float SoundBaselinePercent = 0.15f;

    private float _cooldownRemaining;
    private bool _lightsOut;

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void SyncLightsOut(bool active) => _lightsOut = active;

    public bool CanClap() => _lightsOut && _cooldownRemaining <= 0f;

    public void TryClap()
    {
        if (!CanClap()) return;

        // Authority client requests server.
        if (IsMultiplayerAuthority())
        {
            if (Multiplayer.IsServer())
                ExecuteClap();
            else
                RpcId(NetworkManager.ServerPeerId, nameof(RequestClap));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RequestClap()
    {
        if (!Multiplayer.IsServer()) return;
        var sender = Multiplayer.GetRemoteSenderId();
        if (sender != int.Parse(Owner.Name)) return; // anti-spoof
        ExecuteClap();
    }

    private void ExecuteClap()
    {
        _cooldownRemaining = Cooldown;
        Rpc(nameof(SyncClap));
        // Server also dispatches noise.
        float baseline = VoiceManager.Instance?.BaselineAmplitude ?? 0.05f;
        VoiceManager.Instance?.ReportNoiseEvent(GlobalPosition, 0, ListenerSoundEvent.SoundKind.Special);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void SyncClap()
    {
        _cooldownRemaining = Cooldown;
        // TODO: trigger illumination effect (light node or shader)
        HUDManager.Instance?.SetClapCooldown(Cooldown);
    }

    public override void _Process(double delta)
    {
        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= (float)delta;
    }
}
```

### Lights Out integration

`EscalationTimer` (or `LightingController`) will call `ClapAbility.SyncLightsOut(true)` via RPC when minute-25 event fires. Since `EscalationTimer` doesn't exist yet, stub the call site in `GameManager` or add a TODO comment.

### Acceptance

- [ ] Q does nothing before Lights Out.
- [ ] After Lights Out, Q triggers 0.5s illumination.
- [ ] Cooldown 12s enforced and shown on HUD.
- [ ] Emits Tier 0 noise at player position.

---

## 4. Vocal Sacrifice

### Design reference

- Hold `G` for 1s, then speak loudly (Tier 2+).
- 1s pre-signal: amber pulse on teammate status bar.
- Locks Listener on sacrificing player for 30s.
- Overrides Token targeting, Hunting, Alerted, non-Frenzy sprint.
- Does **not** override Phase 3 Permanent Frenzy or active Scream Frenzy.
- Activation speech is special Tier 2.5: transfers Token, counts as Tier 2 for imprinting, but no Tier 3 side effects.
- 30s amber countdown on all HUDs.
- Grief detection: speech within 2s of lock activation by teammates (excluding activation speech) flags grief.

### Files

- New: `Scripts/Player/VocalSacrifice.cs`
- Modify: `Scripts/Player/PlayerController.cs`
- Modify: `Scripts/Enemy/ListenerAI.cs` (sacrifice lock hooks)
- Modify: `Scripts/UI/HUDManager.cs` (countdown)

### VocalSacrifice.cs

```csharp
public partial class VocalSacrifice : Node3D
{
    [Export] public float HoldTime = 1f;
    [Export] public float LockDuration = 30f;
    [Export] public float ActivationTierRequirement = 2; // Tier 2+

    private float _holdProgress;
    private bool _holding;
    private float _lockRemaining;
    private bool _awaitingActivationSpeech;
    private float _griefWindowRemaining;

    public bool IsSacrificeActive => _lockRemaining > 0f;
    public float LockRemaining => _lockRemaining;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RequestSacrificePreSignal()
    {
        if (!Multiplayer.IsServer()) return;
        // Validate sender == owner.
        Rpc(nameof(SyncPreSignal));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void SyncPreSignal()
    {
        // HUD amber pulse on teammate bar for 1s.
        HUDManager.Instance?.PulseTeammateForSacrifice(Owner.Name);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RequestSacrificeLock()
    {
        if (!Multiplayer.IsServer()) return;
        // Server validates that owner is alive and not in Phase 3 permanent frenzy.
        if (Owner is PlayerController pc && pc.IsDead) return;
        ExecuteLock();
    }

    private void ExecuteLock()
    {
        _lockRemaining = LockDuration;
        _griefWindowRemaining = 2f;
        Rpc(nameof(SyncSacrificeLock), LockDuration);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void SyncSacrificeLock(float duration)
    {
        _lockRemaining = duration;
        HUDManager.Instance?.ShowSacrificeCountdown(duration, Owner.Name);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_lockRemaining > 0f)
        {
            _lockRemaining -= dt;
            HUDManager.Instance?.UpdateSacrificeCountdown(_lockRemaining);
            if (_griefWindowRemaining > 0f)
                _griefWindowRemaining -= dt;
        }

        if (!_holding || !IsMultiplayerAuthority()) return;

        _holdProgress += dt;
        if (_holdProgress >= HoldTime && !_awaitingActivationSpeech)
        {
            _awaitingActivationSpeech = true;
            if (Multiplayer.IsServer())
                Rpc(nameof(SyncPreSignal));
            else
                RpcId(NetworkManager.ServerPeerId, nameof(RequestSacrificePreSignal));
        }
    }

    public void SetHolding(bool holding)
    {
        _holding = holding;
        if (!holding)
        {
            _holdProgress = 0f;
            _awaitingActivationSpeech = false;
        }
    }

    // Called by VoiceManager on local player when tier reaches Tier 2+.
    public void OnActivationSpeechDetected()
    {
        if (!_awaitingActivationSpeech) return;
        if (Multiplayer.IsServer())
            RequestSacrificeLock();
        else
            RpcId(NetworkManager.ServerPeerId, nameof(RequestSacrificeLock));
        _awaitingActivationSpeech = false;
        _holdProgress = 0f;
    }
}
```

### VoiceManager integration

In `VoiceManager.UpdateTokenHolder()` or tier-change logic, when local player reaches Tier 2+ and has `VocalSacrifice` awaiting activation, call `vocalSacrifice.OnActivationSpeechDetected()`.

Special Tier 2.5 handling:
- The activation speech should transfer Token (normal).
- It should **not** trigger Tier 3 side effects. Since Tier 2 is normal speech, this is automatic — only Tier 3 triggers screen flash / shatter.
- Count as Tier 2 for imprinting (when imprinting exists).

### ListenerAI integration

1. Add to `ListenerAI`:
   - `[Export] public float VocalSacrificeDuration = 30f;`
   - `private Node3D _vocalSacrificeTarget;`
   - `private float _vocalSacrificeTimer;`

2. Add public server API:
   ```csharp
   public void SetVocalSacrificeTarget(Node3D target, float duration)
   {
       if (!Multiplayer.IsServer()) return;
       _vocalSacrificeTarget = target;
       _vocalSacrificeTimer = duration;
       Rpc(nameof(SyncSacrificeTarget), target.GetPath(), duration);
   }

   [Rpc(Authority, CallLocal = true)] private void SyncSacrificeTarget(NodePath path, float duration) { ... }
   ```

3. Override `IsVocalSacrificeLockActive()`:
   ```csharp
   protected override bool IsVocalSacrificeLockActive() => _vocalSacrificeTimer > 0f && _vocalSacrificeTarget != null;
   ```

4. In `ResolveFallbackTargetPriority()` / `UpdateStateLogic`, handle `ListenerTargetMode.VocalSacrifice`:
   - If active and target valid, set nav target to target position.
   - If Phase3PermanentFrenzy or ScreamFrenzy active, ignore sacrifice lock.

### HUD integration

- `ShowSacrificeCountdown(float duration, string playerName)`
- `UpdateSacrificeCountdown(float remaining)`
- `PulseTeammateForSacrifice(string playerName)` (amber flash on teammate status bar — if teammate bar doesn't exist yet, show a temporary center-screen banner).

### Acceptance

- [ ] Hold G 1s triggers amber pre-signal.
- [ ] Speaking Tier 2+ activates 30s lock.
- [ ] Listener chases sacrificer regardless of Token holder.
- [ ] Lock does not override Scream Frenzy or Phase 3 Permanent Frenzy.
- [ ] 30s countdown appears on all players' HUD.
- [ ] Teammate speech within 2s of lock flags grief log.

---

## 5. Role ability keys

### Design reference

| Key | Role | Action | Status |
|---|---|---|---|
| F | The Loud | 5s stun pulse, Tier 2.5, 90s cooldown | MVP |
| R | The Static | White-noise bubble, 4m radius, 40s, 2 charges | MVP |
| T | The Echo | Replay decoy | Post-launch, skip |

### 5.1 RoleData

New file: `Scripts/Player/RoleData.cs`

```csharp
public enum PlayerRole { None, Loud, Static, Mute }

public partial class RoleData : Node
{
    [Export] public PlayerRole Role = PlayerRole.None;
    public int StaticChargesRemaining = 2;
}
```

For MVP, role assignment can be a lobby dropdown or `GameManager` can assign roles for testing. At minimum, expose a `[Export]` on `PlayerController` so designers can assign roles in scene.

### 5.2 Loud Stun

New file: `Scripts/Player/Abilities/LoudStun.cs`

- Key: F
- Only usable if `RoleData.Role == PlayerRole.Loud`.
- Cooldown 90s.
- On use:
  1. Emit Tier 2.5 noise at player position (Token transfers, no Tier 3 side effects).
  2. Server tells all Listeners to enter Stunned state for 5s.

ListenerAI changes:
- Add `AIState.Stunned` or use a `_stunTimer` that pauses movement/state logic.
- Add `public void ApplyStun(float duration)` server method with RPC sync.
- In `_PhysicsProcess`, if `_stunTimer > 0`, decrement and return early (no movement/state updates).
- Eye color: white/amber pulse while stunned.

### 5.3 Static Bubble

New file: `Scripts/Player/Abilities/StaticBubble.cs`

- Key: R
- Only usable if `RoleData.Role == PlayerRole.Static` and charges > 0.
- Creates a 4m radius Area3D around player for 40s.
- Visible to all players at 15m (20m post-Lights Out).
- Collapses on player death.
- Audio suppression: override `ListenerAI.IsAudioSuppressedByFutureSystems` to return true if sound origin is inside any active bubble AND the listener is also inside the bubble (or only the player is inside — design says bubble masks sounds inside it).

Implementation:
- StaticBubble as Node3D child of player, Area3D with sphere collision.
- Track active bubbles in a static list `StaticBubble.ActiveBubbles`.
- `ListenerAI.IsAudioSuppressedByFutureSystems(soundEvent)` checks if sound origin is inside any bubble.

### PlayerController integration

```csharp
if (Input.IsActionJustPressed("ability_f")) _loudStun?.TryStun();
if (Input.IsActionJustPressed("ability_r")) _staticBubble?.TryDeploy();
```

### Acceptance

- [ ] F does nothing for non-Loud roles.
- [ ] Loud F stuns all Listeners for 5s; 90s cooldown.
- [ ] R does nothing for non-Static roles.
- [ ] Static R deploys 4m bubble for 40s; consumes 1 of 2 charges.
- [ ] Sounds inside bubble are suppressed for Listener detection.
- [ ] Bubble disappears on Static death.

---

## File inventory

### New files

- `Scripts/Player/GestureSystem.cs`
- `Scripts/Player/Abilities/ClapAbility.cs`
- `Scripts/Player/Abilities/VocalSacrifice.cs`
- `Scripts/Player/Abilities/LoudStun.cs`
- `Scripts/Player/Abilities/StaticBubble.cs`
- `Scripts/Player/RoleData.cs`
- `Scripts/UI/GestureWheel.cs`
- `Scenes/GestureWheel.tscn`

### Modified files

- `Scripts/Player/PlayerController.cs` — input routing, instantiate ability nodes
- `Scripts/Enemy/ListenerAI.cs` — stun, sacrifice lock, static-bubble suppression
- `Scripts/UI/HUDManager.cs` — ability cooldowns, sacrifice countdown, gesture wheel container
- `Scripts/Core/VoiceManager.cs` — activation speech hook for sacrifice
- `Scripts/UI/CustomLobby.cs` — calibration CTA
- `Scripts/UI/MatchmakingLobby.cs` — calibration CTA
- `Scenes/Player.tscn` — add ability nodes
- `Scenes/HUD.tscn` — add gesture wheel, cooldown bars
- `Scenes/CustomLobby.tscn` — add calibration button/progress
- `Scenes/MatchmakingLobby.tscn` — add calibration button/progress

---

## Verification plan

1. Build compilation: `dotnet build LastWord.sln`.
2. Unit test `SequenceManagerTests.cs` still passes.
3. Manual smoke in Godot editor:
   - Open `GameScene.tscn`, press Play.
   - Verify gestures keys do not error.
   - Verify Q is disabled before Lights Out.
   - Verify G hold + Tier 2 locks Listener (debug label shows target mode).
   - Verify F stuns Listener (debug label + frozen movement).
   - Verify R bubble suppresses noise.
