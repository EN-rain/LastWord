using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks per-player vocal imprinting profiles used by the Second Listener
/// targeting logic (§3.4) and the Playback Trap system (§3.5 / §4.4).
/// </summary>
public partial class VocalImprintTracker : Node
{
	[Export] public float MaxImprintSeconds = 30.0f;
	[Export] public float DeadProfileDecayDelay = 60.0f;
	[Export] public float DecayRatePerSecond = 1.0f;

	public class Profile
	{
		public Node3D Player;
		public float CumulativeSpeakTime;
		public double LastSpeakTime;
		public bool IsDead;

		public Profile(Node3D player)
		{
			Player = player;
		}
	}

	private readonly Dictionary<Node3D, Profile> _profiles = new();

	public IReadOnlyCollection<Profile> Profiles => _profiles.Values;

	/// <summary>
	/// Records <paramref name="delta"/> seconds of speaking time for <paramref name="player"/>.
	/// </summary>
	public void RecordSpeaking(Node3D player, float delta, bool isDead = false)
	{
		if (player == null || delta <= 0f)
			return;

		if (!_profiles.TryGetValue(player, out Profile profile))
		{
			profile = new Profile(player);
			_profiles[player] = profile;
		}

		profile.CumulativeSpeakTime += delta;
		profile.LastSpeakTime = Time.GetTicksMsec() / 1000.0;
		profile.IsDead = isDead;
	}

	/// <summary>
	/// Returns the imprint strength for a player in the range [0, 1] based on
	/// cumulative speaking time. Values above 1 are clamped.
	/// </summary>
	public float GetImprintStrength(Node3D player)
	{
		if (player == null || !_profiles.TryGetValue(player, out Profile profile))
			return 0f;

		return Mathf.Min(profile.CumulativeSpeakTime / Mathf.Max(MaxImprintSeconds, 0.01f), 1f);
	}

	/// <summary>
	/// Returns the living player with the highest vocal imprint, optionally
	/// excluding a specific player. Used by the Second Listener (§8).
	/// </summary>
	public Node3D GetMostImprintedLivingTarget(Node3D exclude = null)
	{
		Profile best = null;
		float bestStrength = -1f;

		foreach (Profile profile in _profiles.Values)
		{
			if (profile.Player == null || profile.IsDead)
				continue;
			if (exclude != null && profile.Player == exclude)
				continue;

			float strength = GetImprintStrength(profile.Player);
			if (strength > bestStrength)
			{
				bestStrength = strength;
				best = profile;
			}
		}

		return best?.Player;
	}

	/// <summary>
	/// Marks a player as dead; their profile will begin decaying after
	/// <see cref="DeadProfileDecayDelay"/> seconds.
	/// </summary>
	public void MarkDead(Node3D player)
	{
		if (player == null)
			return;

		if (_profiles.TryGetValue(player, out Profile profile))
			profile.IsDead = true;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		double now = Time.GetTicksMsec() / 1000.0;

		// Decay dead profiles after the configured delay.
		List<Node3D> toRemove = null;
		foreach (Profile profile in _profiles.Values)
		{
			if (!profile.IsDead)
				continue;

			if (now - profile.LastSpeakTime >= DeadProfileDecayDelay)
			{
				profile.CumulativeSpeakTime -= DecayRatePerSecond * dt;
				if (profile.CumulativeSpeakTime <= 0f)
				{
					toRemove ??= new List<Node3D>();
					toRemove.Add(profile.Player);
				}
			}
		}

		if (toRemove != null)
		{
			foreach (Node3D player in toRemove)
				_profiles.Remove(player);
		}
	}
}
