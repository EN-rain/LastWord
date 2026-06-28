using Godot;
using LastWord;

/// <summary>
/// Clap ability (Q key). Available only after Lights Out. Illuminates the room
/// for 0.5 seconds and emits a Tier 0 noise at 15% of calibrated baseline.
/// </summary>
public partial class ClapAbility : Node3D
{
	[Export] public float Cooldown = 12.0f;
	[Export] public float IlluminateDuration = 0.5f;
	[Export] public float SoundRadius = 4.0f;
	[Export] public float SoundBaselinePercent = 0.15f;

	/// <summary>
	/// Global flag set by the escalation/lighting system when Lights Out begins.
	/// </summary>
	public static bool LightsOutActive { get; set; } = false;

	private float _cooldownRemaining;
	private float _illuminateRemaining;
	private OmniLight3D _light;

	public bool IsOnCooldown => _cooldownRemaining > 0f;
	public float CooldownRemaining => _cooldownRemaining;

	public override void _Ready()
	{
		_light = GetNodeOrNull<OmniLight3D>("ClapLight");
		if (_light != null)
		{
			_light.Visible = false;
			_light.LightEnergy = 0f;
			_light.OmniRange = SoundRadius;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (_cooldownRemaining > 0f)
			_cooldownRemaining -= dt;

		if (_illuminateRemaining > 0f)
		{
			_illuminateRemaining -= dt;
			if (_illuminateRemaining <= 0f && _light != null)
			{
				_light.Visible = false;
				_light.LightEnergy = 0f;
			}
		}
	}

	/// <summary>
	/// Called by the local authority PlayerController when Q is pressed.
	/// </summary>
	public void TryClap()
	{
		if (!LightsOutActive)
			return;

		if (IsOnCooldown)
			return;

		if (!IsMultiplayerAuthority())
			return;

		if (Multiplayer.IsServer())
		{
			ExecuteClap();
		}
		else
		{
			RpcId(NetworkManager.ServerPeerId, nameof(RequestClap));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void RequestClap()
	{
		if (!Multiplayer.IsServer())
			return;

		long senderId = Multiplayer.GetRemoteSenderId();
		if (senderId != GetMultiplayerAuthority())
			return;

		ExecuteClap();
	}

	private void ExecuteClap()
	{
		_cooldownRemaining = Cooldown;
		_illuminateRemaining = IlluminateDuration;

		AudioAssets.PlayOneShot3D(AudioAssets.AbilityClap, this, GlobalPosition, "SFX");

		Rpc(nameof(SyncClap), Cooldown);

		ReportClapNoise();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SyncClap(float cooldown)
	{
		_cooldownRemaining = cooldown;
		_illuminateRemaining = IlluminateDuration;

		AudioAssets.PlayOneShot3D(AudioAssets.AbilityClap, this, GlobalPosition, "SFX");

		if (_light != null)
		{
			_light.Visible = true;
			_light.LightEnergy = 1f;
			_light.OmniRange = SoundRadius;
		}

		HUDManager.Instance?.UpdatePlayerState("Clap!", new Color(0.9f, 0.9f, 0.7f));
	}

	private void ReportClapNoise()
	{
		if (VoiceManager.Instance == null)
			return;

		float baseline = Mathf.Max(VoiceManager.Instance.BaselineAmplitude, 0.001f);
		float syntheticAmplitude = baseline * Mathf.Max(SoundBaselinePercent, 0f);
		int tier = syntheticAmplitude >= baseline ? 1 : 0;

		VoiceManager.Instance.ReportNoiseEvent(
			GlobalPosition,
			tier,
			SoundKind.Special,
			source: this,
			isSpecialLongRange: false);
	}
}
