using Godot;
using LastWord;
using System;

public partial class RegistrationBoard : Area3D
{
    [Export] public float DetectionRadius { get; set; } = 3.0f;
    [Export] public WordRegistry WordRegistry { get; set; }

    [Signal] public delegate void WordRegistrationRequestedEventHandler(
        string word, string peerName, long peerId);

    private PlayerController _localPlayerInside = null;
    private PlayerController _progressPlayer = null;

    public override void _Ready()
    {
        WordRegistry ??= WordRegistry.Instance;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        var vm = VoiceManager.Instance;
        if (vm != null)
        {
            vm.RecognizedSpeechSubmitted += OnRecognizedSpeechSubmitted;
        }
        else
        {
            // VoiceManager not yet ready; defer one frame and retry.
            CallDeferred(nameof(ConnectVoiceManager));
        }
    }

    private void ConnectVoiceManager()
    {
        var vm = VoiceManager.Instance;
        if (vm != null)
            vm.RecognizedSpeechSubmitted += OnRecognizedSpeechSubmitted;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;
        if (VoiceManager.Instance != null)
            VoiceManager.Instance.RecognizedSpeechSubmitted -= OnRecognizedSpeechSubmitted;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController pc && pc.IsMultiplayerAuthority())
        {
            _localPlayerInside = pc;
            pc.GetNodeOrNull<MuteSilentDrop>("MuteSilentDrop")?.OnEnterBoard(this);
            pc.GetNodeOrNull<ArchivistRegistration>("ArchivistRegistration")?.OnEnterBoard(this);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController pc && pc == _localPlayerInside)
        {
            pc.GetNodeOrNull<MuteSilentDrop>("MuteSilentDrop")?.OnExitBoard();
            pc.GetNodeOrNull<ArchivistRegistration>("ArchivistRegistration")?.OnExitBoard();
            _localPlayerInside = null;
        }
    }

    public void SetProgress(float progress)
    {
        // TODO: drive a progress bar overlay once the RegistrationBoard UI exists.
        if (progress >= 1f)
            progress = 0f;
    }

    /// <summary>
    /// Mute Silent Drop (§5.Mute): deposit the held note without speaking.
    /// </summary>
    public void SilentDrop(PlayerController player)
    {
        if (player == null) return;
        if (!OverlapsBody(player)) return;
        RegisterHeldNoteFor(player);
    }

    /// <summary>
    /// Archivist silent registration (§5.Archivist): register by holding E for 5s.
    /// </summary>
    public void RegisterSilently(PlayerController player)
    {
        if (player == null) return;
        if (!OverlapsBody(player)) return;
        RegisterHeldNoteFor(player);
    }

    private void RegisterHeldNoteFor(PlayerController player)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        long peerId = ResolvePeerId(player);
        var held = gm.GetHeldNote(peerId);
        if (string.IsNullOrEmpty(held.word)) return;

        EmitSignal(SignalName.WordRegistrationRequested, held.word, player.Name, peerId);
        (WordRegistry ?? WordRegistry.Instance)?.RegisterWord(held.word, peerId);
        AudioAssets.PlayOneShot3D(AudioAssets.LockOpen01, player, player.GlobalPosition, "SFX");
        gm.ClearHeldNote(peerId);
    }

    private void OnRecognizedSpeechSubmitted(long peerId, string spokenWord, int tier)
    {
        if (tier < 1) return; // Only Tier 1+ (whisper+) can register words
        if (peerId <= 0) return;

        var player = FindPlayerByPeerId(peerId);
        if (player == null) return;
        if (!OverlapsBody(player)) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        var held = gm.GetHeldNote(peerId);
        if (string.IsNullOrEmpty(held.word)) return;
        if (!string.Equals(spokenWord?.Trim(), held.word, StringComparison.OrdinalIgnoreCase))
            return;

        EmitSignal(SignalName.WordRegistrationRequested, held.word, player.Name, peerId);
        (WordRegistry ?? WordRegistry.Instance)?.RegisterWord(held.word, peerId);
        AudioAssets.PlayOneShot3D(AudioAssets.LockOpen01, player, player.GlobalPosition, "SFX");
        gm.ClearHeldNote(peerId);
    }

    private static long ResolvePeerId(PlayerController player)
    {
        if (player == null)
            return 0;
        return long.TryParse(player.Name, out long peerId)
            ? peerId
            : player.GetMultiplayerAuthority();
    }

    private PlayerController FindPlayerByPeerId(long peerId)
    {
        foreach (Node node in GetTree().GetNodesInGroup("Player"))
        {
            if (node is not PlayerController player)
                continue;
            if (ResolvePeerId(player) == peerId)
                return player;
        }
        return null;
    }
}
