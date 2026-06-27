using Godot;
using System;
using System.Collections.Generic;

public partial class SequenceManager : Node
{
	[Signal] public delegate void SequenceRevealedEventHandler(string[] sequence);
	[Signal] public delegate void WordAcceptedEventHandler(string word, int index);
	[Signal] public delegate void WordRejectedEventHandler(string word, string expected);
	[Signal] public delegate void SequenceResetEventHandler();
	[Signal] public delegate void SequenceLockedEventHandler(float duration);
	[Signal] public delegate void SequenceCompleteEventHandler();

	[Export] public float SustainedTierDuration = 0.5f;
	[Export] public float LockDuration = 30.0f;

	private readonly List<string> _sequence = new();
	private int _currentIndex;
	private double _sustainedTimer;
	private double _lockTimer;

	public bool IsLocked => _lockTimer > 0.0;
	public bool IsComplete { get; private set; }
	public bool AllWordsRegistered => WordRegistry.Instance?.RegisteredWords.Count >= (WordRegistry.Instance?.TotalWords ?? int.MaxValue);
	public int CurrentIndex => _currentIndex;
	public IReadOnlyList<string> CurrentSequence => _sequence;

	public override void _Process(double delta)
	{
		if (!IsLocked)
			return;

		_lockTimer -= delta;
		if (_lockTimer <= 0.0)
		{
			_lockTimer = 0.0;
			ResetSequence();
		}
	}

	public void GenerateSequence(IReadOnlyList<string> words, int playerCount)
	{
		int length = Mathf.Min(playerCount, words?.Count ?? 0);

		_sequence.Clear();
		if (words != null && words.Count > 0)
		{
			var pool = new List<string>(words);
			Shuffle(pool);

			for (int i = 0; i < length; i++)
				_sequence.Add(pool[i]);
		}

		_currentIndex = 0;
		_sustainedTimer = 0.0;
		IsComplete = _sequence.Count == 0;

		EmitSignal(SignalName.SequenceRevealed, _sequence.ToArray());
		if (IsComplete)
			EmitSignal(SignalName.SequenceComplete);
	}

	public void GenerateSequenceFromRegistry(int playerCount)
	{
		var words = WordRegistry.Instance?.RegisteredWords;
		GenerateSequence(words, playerCount);
	}

	public void OnVoiceUpdate(string spokenWord, int currentTier, double delta)
	{
		if (IsLocked || IsComplete)
			return;

		ValidateWord(spokenWord, currentTier, delta);
	}

	public void SubmitRecognizedWord(string spokenWord, int currentTier)
	{
		if (IsLocked || IsComplete)
			return;

		ValidateWord(spokenWord, currentTier, SustainedTierDuration);
	}

	public void ResetSequence()
	{
		_currentIndex = 0;
		_sustainedTimer = 0.0;
		EmitSignal(SignalName.SequenceReset);
	}

	public void LockSequence()
	{
		_lockTimer = LockDuration;
		EmitSignal(SignalName.SequenceLocked, LockDuration);
	}

	private void ValidateWord(string spokenWord, int currentTier, double delta)
	{
		if (currentTier < 2)
		{
			_sustainedTimer = 0.0;
			return;
		}

		_sustainedTimer += delta;
		if (_sustainedTimer < SustainedTierDuration)
			return;

		_sustainedTimer = 0.0;

		if (_currentIndex >= _sequence.Count)
		{
			IsComplete = true;
			EmitSignal(SignalName.SequenceComplete);
			return;
		}

		string expected = _sequence[_currentIndex];
		string candidate = spokenWord?.Trim() ?? string.Empty;

		if (string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase))
		{
			EmitSignal(SignalName.WordAccepted, candidate, _currentIndex);
			_currentIndex++;

			if (_currentIndex >= _sequence.Count)
			{
				IsComplete = true;
				EmitSignal(SignalName.SequenceComplete);
			}
		}
		else
		{
			EmitSignal(SignalName.WordRejected, candidate, expected);
			ResetSequence();
			LockSequence();
		}
	}

	private void Shuffle(List<string> list)
	{
		var rng = new Random();
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
