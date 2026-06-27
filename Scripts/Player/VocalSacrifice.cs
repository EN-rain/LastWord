using Godot;
using LastWord;

/// <summary>
/// Vocal Sacrifice ability (hold G 1s, then speak Tier 2+). Locks the Listener
/// onto the sacrificing player for 30 seconds, overriding Token targeting.
/// Does not override Phase 3 Permanent Frenzy or active Scream Frenzy.
/// </summary>
public partial class VocalSacrifice : Node3D
{
	[Export] public float HoldTime = 1.0f;
	[Export] public float LockDuration = 30.0f;
	[Export] public float GriefWindow = 2.0f;

	/// <summary>
	/// The currently active sacrifice lock in the session, or null if none.
	/// </summary>
	public static VocalSacrifice ActiveLock { get; private set; }

	private float _holdProgress;
	private bool _holding;
	private bool _preSignalled;
	private bool _awaitingActivationSpeech;
	private float _lockRemaining;
	private float _griefWindowRemaining;

	public bool IsLocked => _lockRemaining > 0f;
	public float LockRemaining => _lockRemaining;
	public bool IsAwaitingActivationSpeech => _awaitingActivationSpeech;

	public override void _Ready()
	{
		if (VoiceManager.Instance != null)
			VoiceManager.Instance.TierChanged += OnTierChanged;
	}

	public override void _ExitTree()
	{
		if (VoiceManager.Instance != null)
			VoiceManager.Instance.TierChanged -= OnTierChanged;
	}

	private void OnTierChanged(int newTier)
	{
		if (!_awaitingActivationSpeech)
			return;

		if (newTier >= (int)VoiceTier.Normal)
			OnActivationSpeechDetected();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_lockRemaining > 0f)
		{
			_lockRemaining -= dt;
			if (ActiveLock == this && _lockRemaining <= 0f)
			{
				ActiveLock = null;
				Rpc(nameof(SyncSacrificeEnded));
			}
		}

		if (_griefWindowRemaining > 0f)
			_griefWindowRemaining -= dt;

		if (!_holding || !IsMultiplayerAuthority())
			return;

		_holdProgress += dt;
		if (_holdProgress >= HoldTime && !_preSignalled)
		{
			_preSignalled = true;
			_awaitingActivationSpeech = true;
			if (Multiplayer.IsServer())
				Rpc(nameof(SyncPreSignal));
			else
				RpcId(NetworkManager.ServerPeerId, nameof(RequestPreSignal));
		}
	}

	public void SetHolding(bool holding)
	{
		if (holding == _holding)
			return;

		_holding = holding;
		if (!holding)
		{
			_holdProgress = 0f;
			_preSignalled = false;
			_awaitingActivationSpeech = false;
		}
	}

	/// <summary>
	/// Called by VoiceManager on the authority player when Tier 2+ speech is
	/// detected while we are awaiting the activation speech.
	/// </summary>
	public void OnActivationSpeechDetected()
	{
		if (!_awaitingActivationSpeech || !IsMultiplayerAuthority())
			return;

		if (Multiplayer.IsServer())
			RequestLock();
		else
			RpcId(NetworkManager.ServerPeerId, nameof(RequestLock));

		_awaitingActivationSpeech = false;
		_holdProgress = 0f;
		_preSignalled = false;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestPreSignal()
	{
		if (!Multiplayer.IsServer()) return;
		long senderId = Multiplayer.GetRemoteSenderId();
		if (senderId != GetMultiplayerAuthority()) return;
		Rpc(nameof(SyncPreSignal));
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncPreSignal()
	{
		HUDManager.Instance?.PulseTeammateForSacrifice(Owner.Name);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestLock()
	{
		if (!Multiplayer.IsServer()) return;
		long senderId = Multiplayer.GetRemoteSenderId();
		if (senderId != GetMultiplayerAuthority()) return;

		if (Owner is PlayerController pc && pc.IsDead)
			return;

		ExecuteLock();
	}

	private void ExecuteLock()
	{
		_lockRemaining = LockDuration;
		_griefWindowRemaining = GriefWindow;
		ActiveLock = this;
		AudioAssets.PlayOneShot3D(AudioAssets.AbilityVocalSacrifice, this, GlobalPosition, "SFX");
		Rpc(nameof(SyncSacrificeLock), LockDuration, Owner.GetPath());
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncSacrificeLock(float duration, NodePath targetPath)
	{
		_lockRemaining = duration;
		_griefWindowRemaining = GriefWindow;
		ActiveLock = this;
		AudioAssets.PlayOneShot3D(AudioAssets.AbilityVocalSacrifice, this, GlobalPosition, "SFX");
		HUDManager.Instance?.ShowSacrificeCountdown(duration, Owner?.Name ?? "Unknown");

		Node target = GetNodeOrNull(targetPath);
		if (target != null)
		{
			foreach (var listener in GetTree().GetNodesInGroup("Listener"))
			{
				if (listener is ListenerAI ai)
					ai.SetVocalSacrificeTarget(target as Node3D, duration);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncSacrificeEnded()
	{
		if (ActiveLock == this)
			ActiveLock = null;
		HUDManager.Instance?.HideSacrificeCountdown();
	}

	/// <summary>
	/// Returns true if a teammate speaking right now should be flagged for grief.
	/// The activation speech itself is exempt.
	/// </summary>
	public static bool IsGriefWindowActiveForTeammate(long peerId)
	{
		if (ActiveLock == null) return false;
		if (ActiveLock._griefWindowRemaining <= 0f) return false;
		if (ActiveLock.GetMultiplayerAuthority() == peerId) return false;
		return true;
	}
}
