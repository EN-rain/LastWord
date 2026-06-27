using Godot;
using System;
using System.Collections.Generic;

public partial class WordRegistry : Node
{
    public static WordRegistry Instance { get; set; }

    private readonly List<string> _registeredWords = new();
    private readonly HashSet<string> _registeredSet = new(StringComparer.OrdinalIgnoreCase);

    [Signal] public delegate void RegisteredWordEventHandler(string word, string peerName);
    [Signal] public delegate void AllWordsRegisteredEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public IReadOnlyList<string> RegisteredWords => _registeredWords;
    public int TotalWords { get; set; } = 4;

    public void RegisterWord(string word, long peerId)
    {
        if (string.IsNullOrWhiteSpace(word)) return;
        var normalized = word.Trim().ToLowerInvariant();
        if (_registeredSet.Contains(normalized)) return; // Idempotent
        _registeredSet.Add(normalized);
        _registeredWords.Add(normalized);
        EmitSignal(SignalName.RegisteredWord, normalized, GetPeerName(peerId));
        if (_registeredWords.Count >= TotalWords)
        {
            EmitSignal(SignalName.AllWordsRegistered);
        }
    }

    private string GetPeerName(long peerId)
    {
        // Fallback: use peerId as string; GameManager can override
        return $"Player_{peerId}";
    }

    public void Clear()
    {
        _registeredWords.Clear();
        _registeredSet.Clear();
    }
}
