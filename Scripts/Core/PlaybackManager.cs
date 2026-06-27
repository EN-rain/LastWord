using Godot;
using System;
using System.Collections.Generic;
using LastWord.Core;

/// <summary>
/// Playback Trap / voice mimicry system (§3.5 / §4.4).
/// After a configured period of player silence the manager can spawn a
/// distorted replay of a recorded voice clip near a random player to bait
/// the Listener.
/// </summary>
public partial class PlaybackManager : Node3D
{
	[Export] public float SilenceThreshold = 90.0f;
	[Export] public float PostRandomMatchmakingDelay = 600.0f;
	[Export] public float MimicryPitchMin = 0.7f;
	[Export] public float MimicryPitchMax = 1.3f;
	[Export] public float MimicryDistanceMin = 2.0f;
	[Export] public float MimicryDistanceMax = 6.0f;
	[Export] public bool TriggerEnabled = false;

	private float _silenceTimer = 0f;
	private bool _randomMatchmakingDelayPassed = false;
	private double _runStartTime = 0.0;

	public float SilenceTimer => _silenceTimer;

	public override void _Ready()
	{
		_runStartTime = Time.GetTicksMsec() / 1000.0;
	}

	public override void _Process(double delta)
	{
		if (!TriggerEnabled)
			return;

		float dt = (float)delta;
		_silenceTimer += dt;

		double now = Time.GetTicksMsec() / 1000.0;
		if (!_randomMatchmakingDelayPassed && now - _runStartTime >= PostRandomMatchmakingDelay)
		{
			_randomMatchmakingDelayPassed = true;
			GD.Print("PlaybackManager: 10-minute random-matchmaking delay passed; mimicry can now trigger.");
		}

		if (_silenceTimer >= SilenceThreshold && _randomMatchmakingDelayPassed)
		{
			_silenceTimer = 0f;
			TryTriggerMimicry();
		}
	}

	/// <summary>
	/// Call this whenever any player speaks above Silent tier to reset the silence clock.
	/// </summary>
	public void ReportSpeech()
	{
		_silenceTimer = 0f;
	}

	private void TryTriggerMimicry()
	{
		if (GameManager.Instance == null)
			return;

		VoiceRecorder recorder = FindAnyVoiceRecorder();
		if (recorder == null)
			return;

		AudioStreamWav clip = recorder.GetRecentRecording(10f);
		if (clip == null)
			return;

		Node3D target = FindRandomLivingPlayer();
		if (target == null)
			return;

		Vector3 offset = new Vector3(
			GD.Randf() * 2f - 1f,
			0f,
			GD.Randf() * 2f - 1f
		).Normalized() * (float)GD.RandRange(MimicryDistanceMin, MimicryDistanceMax);

		Vector3 origin = target.GlobalPosition + offset;

		AudioStreamPlayer3D player = new();
		player.Name = "PlaybackTrapMimicry";
		player.Stream = clip;
		player.PitchScale = (float)GD.RandRange(MimicryPitchMin, MimicryPitchMax);
		player.MaxDistance = 20f;
		player.GlobalPosition = origin;
		GetTree().CurrentScene?.AddChild(player);
		player.Play();
		player.Finished += () => player.QueueFree();

		GD.Print($"PlaybackManager: mimicry playback at {origin} near {target.Name}.");

		// Achievement hook: Method Actor — the Listener played someone's recorded voice at a teammate.
		AchievementManager.Instance?.Unlock(AchievementManager.Id.MethodActor);
	}

	private VoiceRecorder FindAnyVoiceRecorder()
	{
		foreach (Node node in GetTree().GetNodesInGroup("Player"))
		{
			if (node is PlayerController controller && controller.HasNode("VoiceRecorder"))
			{
				VoiceRecorder rec = controller.GetNode<VoiceRecorder>("VoiceRecorder");
				if (rec != null)
					return rec;
			}
		}
		return null;
	}

	private Node3D FindRandomLivingPlayer()
	{
		List<PlayerController> living = new();
		foreach (Node node in GetTree().GetNodesInGroup("Player"))
		{
			if (node is PlayerController controller && !controller.IsDead)
				living.Add(controller);
		}

		if (living.Count == 0)
			return null;

		return living[GD.RandRange(0, living.Count - 1)];
	}
}
