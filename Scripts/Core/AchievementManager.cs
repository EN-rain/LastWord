using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LastWord.Core;

/// <summary>
/// Tracks and persists funny little "achievements" the player can earn during a run.
/// Wired up as an autoload — call <see cref="Unlock"/> from anywhere with an ID and a joke,
/// and the manager handles dedup, save, and the on-screen toast.
///
/// Achievement IDs are stable strings — never rename an existing ID or you orphan saves.
/// </summary>
public partial class AchievementManager : Node
{
    public static AchievementManager Instance { get; private set; }

    /// <summary>Emitted whenever a NEW achievement is unlocked this session.</summary>
    [Signal] public delegate void AchievementUnlockedEventHandler(string id, string title, string flavor);

    // ---- The Funny Eight ----------------------------------------------------------------
    public enum Id
    {
        ItSeesYou,         // 👂 Listener locked onto you
        MimeTime,          // 🤐 Finished Phase 1 without ever raising your voice above a whisper
        WrongNumber,       // 📻 Used intercom/phone and the Listener went to the wrong room
        LastBreath,        // 🫁 Vocal Sacrifice killed the Listener — the ultimate mic drop
        MethodActor,       // 🎭 Listener played your own voice back at you
        MarathonOfSilence, // 🏃 Survived a full match on the Mute role
        FinalGirl,         // 💀 Last surviving player on your team
        TheLastWord,       // 🏆 You literally spoke the final word that ended the run
    }

    /// <summary>Display data for each achievement. Keep the jokes short and punchy.</summary>
    private static readonly Dictionary<Id, (string Title, string Flavor, string Hint)> Catalog = new()
    {
        [Id.ItSeesYou] = (
            "👂 It Sees You",
            "The Listener found you. It's been listening the whole time, actually.",
            "Get detected by the Listener for the first time."
        ),
        [Id.MimeTime] = (
            "🤐 Mime Time",
            "You completed Phase 1 in complete silence. Marcel Marceau is proud. The Listener is suspicious.",
            "Finish Phase 1 without ever speaking above a Whisper."
        ),
        [Id.WrongNumber] = (
            "📻 Wrong Number",
            "You called the kitchen. The Listener went to the attic. Telephony has never been so lethal.",
            "Trigger a radio/intercom broadcast while no teammate is at the receiver."
        ),
        [Id.LastBreath] = (
            "🫁 Last Breath",
            "Used your final breath to kill the Listener. That is the most metal thing a throat can do.",
            "Kill the Listener with a Vocal Sacrifice."
        ),
        [Id.MethodActor] = (
            "🎭 Method Actor",
            "The Listener learned your voice and used it against you. Imitation is the sincerest form of screaming.",
            "Trigger a Playback of your own imprint at full volume."
        ),
        [Id.MarathonOfSilence] = (
            "🏃 Marathon of Silence",
            "Full match as Mute. No talking. Ever. You didn't even whisper to yourself. Heroic.",
            "Survive an entire match while your selected role is Mute."
        ),
        [Id.FinalGirl] = (
            "💀 Final Girl",
            "Everyone else is dead. Just you and the Listener now. Sequel energy.",
            "Be the last surviving player when the team would otherwise be wiped."
        ),
        [Id.TheLastWord] = (
            "🏆 The Last Word",
            "You literally spoke the final word of a winning run. The title of the game was a spoiler the whole time.",
            "Be the speaker of the final note of a successful broadcast."
        ),
    };

    // ---- Runtime state ------------------------------------------------------------------
    private readonly HashSet<string> _unlocked = new();
    private const string SavePath = "user://achievements.json";

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Load();

        // Hook GameManager signals that exist right now — these are the easy wins.
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.Victory += () => OnRunEnded(victory: true);
            gm.RunFailed += _ => OnRunEnded(victory: false);
            gm.PhaseChanged += OnPhaseChanged;
        }
    }

    private void OnPhaseChanged(GameManager.GamePhase phase)
    {
        // Mime Time: if you make it to Phase 2 without ever raising above a Whisper, you win it.
        if (phase == GameManager.GamePhase.Phase2 && !IsUnlocked(Id.MimeTime))
        {
            var vm = VoiceManager.Instance;
            if (vm != null && vm.MaxTierReachedThisRun <= VoiceTier.Whisper)
                Unlock(Id.MimeTime);
        }
    }

    /// <summary>Call this from any system with a stable <see cref="Id"/> value.</summary>
    public void Unlock(Id id, bool force = false)
    {
        var key = id.ToString();
        if (!force && _unlocked.Contains(key)) return;
        _unlocked.Add(key);

        var (title, flavor, _) = Catalog[id];
        GD.Print($"[Achievement] Unlocked: {title} — {flavor}");
        EmitSignal(SignalName.AchievementUnlocked, key, title, flavor);
        Save();
    }

    /// <summary>String-based convenience overload for trigger sites that already have the string.</summary>
    public void Unlock(string id)
    {
        if (Enum.TryParse<Id>(id, out var parsed)) Unlock(parsed);
        else GD.PushWarning($"[Achievement] Unknown id '{id}'");
    }

    public bool IsUnlocked(Id id) => _unlocked.Contains(id.ToString());

    public IReadOnlyCollection<string> GetAllUnlocked() => _unlocked.ToArray();

    // ---- Hook helpers (one-liners for systems that want to fire an achievement by name) ----
    /// <summary>
    /// Call from RadioBroadcast (or its proxy) when a broadcast is sent but no teammate
    /// is at the receiver, so the Listener is attracted to the wrong room.
    /// TODO: wire when RadioBroadcast.cs is promoted from a runtime stub to a real class.
    /// </summary>
    public void UnlockWrongNumber() => Unlock(Id.WrongNumber);

    /// <summary>Call from PlayerController when its owner becomes the last living teammate.</summary>
    public void UnlockFinalGirl() => Unlock(Id.FinalGirl);

    /// <summary>Reset on-run counters when a fresh run starts. Does NOT clear unlocked set.</summary>
    public void OnRunStart()
    {
        VoiceManager.Instance?.ResetRunTracking();
    }

    private void OnRunEnded(bool victory)
    {
        // The Last Word: you spoke the very last word of a winning run.
        if (victory)
        {
            var vm = VoiceManager.Instance;
            if (vm != null && vm.SecondsSinceLastVoice <= 5.0f)
                Unlock(Id.TheLastWord);
        }

        // Marathon of Silence: full match on Mute role AND we survived to the end.
        if (victory)
        {
            var role = GetLocalRole();
            if (role == PlayerRole.Mute)
                Unlock(Id.MarathonOfSilence);
        }
    }

    private static PlayerRole GetLocalRole()
    {
        var tree = Instance?.GetTree();
        if (tree == null) return PlayerRole.None;
        var player = tree.GetFirstNodeInGroup("Player") as Node;
        var rd = player?.GetNodeOrNull<RoleData>("RoleData");
        return rd?.Role ?? PlayerRole.None;
    }

    // ---- Persistence --------------------------------------------------------------------
    private void Save()
    {
        try
        {
            using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (f == null) return;
            f.StoreString(Json.Stringify(_unlocked.ToArray()));
        }
        catch (Exception e) { GD.PushWarning($"[Achievement] Save failed: {e.Message}"); }
    }

    private void Load()
    {
        if (!FileAccess.FileExists(SavePath)) return;
        try
        {
            using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            if (f == null) return;
            var raw = f.GetAsText();
            if (string.IsNullOrWhiteSpace(raw)) return;
            var arr = Json.ParseString(raw).AsGodotArray();
            foreach (var v in arr) _unlocked.Add(v.AsString());
        }
        catch (Exception e) { GD.PushWarning($"[Achievement] Load failed: {e.Message}"); }
    }
}
