using Godot;

namespace LastWord
{
    /// <summary>
    /// Central registry of all theme-matched audio assets for *Last Word*.
    /// Paths are relative to <c>res://</c> so they can be loaded with
    /// <see cref="GD.Load{T}(string)"/> or assigned directly to
    /// <see cref="AudioStreamPlayer.Stream"/>.
    /// </summary>
    public static class AudioAssets
    {
        // ------------------------------------------------------------------
        // Listener
        // ------------------------------------------------------------------
        public const string ListenerHum = "res://Assets/Audio/listener/listener_hum.wav";
        public const string ListenerAlertClick = "res://Assets/Audio/listener/listener_alert_click.wav";
        public const string ListenerHuntingBreath = "res://Assets/Audio/listener/listener_hunting_breath.wav";
        public const string ListenerFrenzyTone = "res://Assets/Audio/listener/listener_frenzy_tone.wav";
        public const string ListenerCatchSilence = "res://Assets/Audio/listener/listener_catch_silence.wav";

        // ------------------------------------------------------------------
        // Player
        // ------------------------------------------------------------------
        public const string FootstepWalk01 = "res://Assets/Audio/player/footstep_walk_01.ogg";
        public const string FootstepWalk02 = "res://Assets/Audio/player/footstep_walk_02.ogg";
        public const string FootstepRun01 = "res://Assets/Audio/player/footstep_run_01.ogg";
        public const string FootstepRun02 = "res://Assets/Audio/player/footstep_run_02.ogg";
        public const string FootstepWood01 = "res://Assets/Audio/player/footstep_wood_01.ogg";
        public const string FootstepWood02 = "res://Assets/Audio/player/footstep_wood_02.ogg";
        public const string Landing = "res://Assets/Audio/player/landing.wav";

        // ------------------------------------------------------------------
        // Abilities
        // ------------------------------------------------------------------
        public const string AbilityClap = "res://Assets/Audio/abilities/ability_clap.wav";
        public const string AbilityLoudStun = "res://Assets/Audio/abilities/ability_loud_stun.wav";
        public const string AbilityStaticBubble = "res://Assets/Audio/abilities/ability_static_bubble.wav";
        public const string AbilityVocalSacrifice = "res://Assets/Audio/abilities/ability_vocal_sacrifice.wav";
        public const string AbilityWitnessBurst = "res://Assets/Audio/abilities/ability_witness_burst.wav";

        // ------------------------------------------------------------------
        // Ambience
        // ------------------------------------------------------------------
        public const string AmbienceLoop01 = "res://Assets/Audio/ambience/ambience_loop_01.ogg";
        public const string MachineLoop01 = "res://Assets/Audio/ambience/machine_loop_01.ogg";
        public const string WindAmbience = "res://Assets/Audio/ambience/wind_ambience.wav";

        // ------------------------------------------------------------------
        // World / environmental
        // ------------------------------------------------------------------
        public const string ClockBell = "res://Assets/Audio/world/clock_bell.wav";
        public const string Creak = "res://Assets/Audio/world/creak.wav";
        public const string GramophoneMusicLoop = "res://Assets/Audio/world/gramophone_music_loop.wav";
        public const string DoorOpen01 = "res://Assets/Audio/world/door_open_01.ogg";
        public const string DoorClose01 = "res://Assets/Audio/world/door_close_01.ogg";
        public const string GlassShatter01 = "res://Assets/Audio/world/glass_shatter_01.ogg";
        public const string GlassShatter02 = "res://Assets/Audio/world/glass_shatter_02.ogg";
        public const string Impact01 = "res://Assets/Audio/world/impact_01.ogg";
        public const string ItemPickup01 = "res://Assets/Audio/world/item_pickup_01.ogg";
        public const string LockOpen01 = "res://Assets/Audio/world/lock_open_01.ogg";

        // ------------------------------------------------------------------
        // UI
        // ------------------------------------------------------------------
        public const string UiHover = "res://Assets/Audio/ui/ui_hover.wav";
        public const string UiClick = "res://Assets/Audio/ui/ui_click.wav";
        public const string UiBack = "res://Assets/Audio/ui/ui_back.wav";
        public const string UiError = "res://Assets/Audio/ui/ui_error.wav";
        public const string VictoryStinger = "res://Assets/Audio/ui/victory_stinger.wav";
        public const string FailureStinger = "res://Assets/Audio/ui/failure_stinger.wav";

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Loads an <see cref="AudioStream"/> from one of the path constants above.
        /// Logs an error and returns <c>null</c> if the resource is missing.
        /// </summary>
        public static AudioStream Load(string path)
        {
            var stream = GD.Load<AudioStream>(path);
            if (stream == null)
                GD.PushError($"AudioAssets: failed to load '{path}'. Is the file imported?");
            return stream;
        }

        /// <summary>
        /// Picks a random footstep from the available walk set.
        /// </summary>
        public static AudioStream RandomFootstepWalk()
        {
            var rng = new System.Random();
            return rng.Next(2) == 0 ? Load(FootstepWalk01) : Load(FootstepWalk02);
        }

        /// <summary>
        /// Picks a random footstep from the available run set.
        /// </summary>
        public static AudioStream RandomFootstepRun()
        {
            var rng = new System.Random();
            return rng.Next(2) == 0 ? Load(FootstepRun01) : Load(FootstepRun02);
        }

        /// <summary>
        /// Picks a random wood footstep (for F2/F3 carpeted/wood areas).
        /// </summary>
        public static AudioStream RandomFootstepWood()
        {
            var rng = new System.Random();
            return rng.Next(2) == 0 ? Load(FootstepWood01) : Load(FootstepWood02);
        }

        /// <summary>
        /// Picks a random glass-shatter sound for Tier-3 scream physics events.
        /// </summary>
        public static AudioStream RandomGlassShatter()
        {
            var rng = new System.Random();
            return rng.Next(2) == 0 ? Load(GlassShatter01) : Load(GlassShatter02);
        }

        // ------------------------------------------------------------------
        // One-shot helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Plays a 3D one-shot sound at <paramref name="position"/> and queues the
        /// player for deletion when finished. Use for footsteps, impacts, abilities,
        /// and environmental effects.
        /// </summary>
        public static AudioStreamPlayer3D PlayOneShot3D(string path, Node parent, Vector3 position, string bus = "SFX", float pitchScale = 1.0f)
        {
            return PlayOneShot3D(Load(path), parent, position, bus, pitchScale);
        }

        /// <summary>
        /// Overload that accepts an already-loaded <see cref="AudioStream"/>.
        /// </summary>
        public static AudioStreamPlayer3D PlayOneShot3D(AudioStream stream, Node parent, Vector3 position, string bus = "SFX", float pitchScale = 1.0f)
        {
            if (parent == null || stream == null)
                return null;

            var player = new AudioStreamPlayer3D
            {
                Stream = stream,
                Bus = bus,
                PitchScale = pitchScale,
                GlobalPosition = position
            };
            parent.AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
            return player;
        }

        /// <summary>
        /// Plays a 2D one-shot sound and queues the player for deletion when finished.
        /// Use for UI feedback and global stingers.
        /// </summary>
        public static AudioStreamPlayer PlayOneShot2D(string path, Node parent, string bus = "UI", float pitchScale = 1.0f)
        {
            if (parent == null)
                return null;

            var stream = Load(path);
            if (stream == null)
                return null;

            var player = new AudioStreamPlayer
            {
                Stream = stream,
                Bus = bus,
                PitchScale = pitchScale
            };
            parent.AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
            return player;
        }
    }
}
