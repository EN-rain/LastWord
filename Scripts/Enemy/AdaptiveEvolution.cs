using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Dormant adaptive-evolution system for the Listener (§4.3).
/// Tracks run metrics and shifts Listener tuning every 5 minutes.
/// Disabled in EA; kept as a stub that can be enabled post-launch.
/// </summary>
public partial class AdaptiveEvolution : Node
{
	[Export] public bool Enabled = false;
	[Export] public float EvaluationInterval = 300.0f; // 5 minutes
	[Export] public float MaxSpeedMultiplier = 1.5f;
	[Export] public float MaxHearingMultiplier = 1.4f;
	[Export] public float MaxAttackRangeMultiplier = 1.3f;

	// Metric A: average player survival time
	public float AverageSurvivalTime { get; private set; }
	// Metric B: average token hold time
	public float AverageTokenHoldTime { get; private set; }
	// Metric C: average cumulative speak time
	public float AverageSpeakTime { get; private set; }
	// Metric D: run success rate (0-1)
	public float SuccessRate { get; private set; }

	public float SpeedMultiplier { get; private set; } = 1f;
	public float HearingMultiplier { get; private set; } = 1f;
	public float AttackRangeMultiplier { get; private set; } = 1f;

	private float _evaluationTimer = 0f;
	private readonly List<float> _survivalSamples = new();
	private readonly List<float> _tokenHoldSamples = new();
	private readonly List<float> _speakTimeSamples = new();
	private int _successCount = 0;
	private int _totalRuns = 0;

	public void RecordDeath(float survivalSeconds, float tokenHoldSeconds, float speakSeconds)
	{
		if (survivalSeconds > 0f)
			_survivalSamples.Add(survivalSeconds);
		if (tokenHoldSeconds >= 0f)
			_tokenHoldSamples.Add(tokenHoldSeconds);
		if (speakSeconds >= 0f)
			_speakTimeSamples.Add(speakSeconds);

		RecalculateAverages();
	}

	public void RecordRunOutcome(bool victory)
	{
		_totalRuns++;
		if (victory)
			_successCount++;
		SuccessRate = _totalRuns > 0 ? (float)_successCount / _totalRuns : 0f;
	}

	private void RecalculateAverages()
	{
		AverageSurvivalTime = AverageOf(_survivalSamples);
		AverageTokenHoldTime = AverageOf(_tokenHoldSamples);
		AverageSpeakTime = AverageOf(_speakTimeSamples);
	}

	private static float AverageOf(List<float> samples)
	{
		if (samples.Count == 0)
			return 0f;

		float sum = 0f;
		foreach (float v in samples)
			sum += v;
		return sum / samples.Count;
	}

	public override void _Process(double delta)
	{
		if (!Enabled)
			return;

		_evaluationTimer += (float)delta;
		if (_evaluationTimer < EvaluationInterval)
			return;

		_evaluationTimer = 0f;
		Evaluate();
	}

	private void Evaluate()
	{
		// Simple heuristic: harder metrics → harder Listener.
		float difficulty = Mathf.Clamp(
			(AverageSurvivalTime / 300f) * 0.3f +
			(AverageTokenHoldTime / 60f) * 0.3f +
			(AverageSpeakTime / 60f) * 0.2f +
			SuccessRate * 0.2f,
			0f, 1f);

		SpeedMultiplier = Mathf.Lerp(1f, MaxSpeedMultiplier, difficulty);
		HearingMultiplier = Mathf.Lerp(1f, MaxHearingMultiplier, difficulty);
		AttackRangeMultiplier = Mathf.Lerp(1f, MaxAttackRangeMultiplier, difficulty);

		GD.Print($"AdaptiveEvolution: evaluated difficulty {difficulty:P0}; " +
			$"speed={SpeedMultiplier:F2} hearing={HearingMultiplier:F2} range={AttackRangeMultiplier:F2}");
	}
}
