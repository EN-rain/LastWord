using Godot;
using LastWord;
using System;

namespace LastWord.Items;

/// <summary>
/// Breakable window (§10 Items & Environmental). A <see cref="StaticBody3D"/>
/// with a glass mesh + collision. Breaks when:
///   * <see cref="BreakWindow"/> is called explicitly (e.g. by an ability, or
///     by a player throwing something at it), or
///   * any incoming <see cref="RigidBody3D"/> collides with at least
///     <see cref="BreakVelocity"/> relative linear velocity.
/// On break: hides the mesh, optionally spawns shard particles, plays a
/// random glass-shatter SFX, emits a Tier-2 Environment sound event so the
/// Listener investigates, and disables the collision.
/// </summary>
public partial class BreakableWindow : StaticBody3D
{
    [Export] public float BreakVelocity { get; set; } = 4.0f;
    [Export] public int NoiseTier { get; set; } = 2;

    [Export] public MeshInstance3D WindowMesh { get; set; }
    [Export] public CollisionShape3D WindowCollision { get; set; }
    [Export] public GpuParticles3D ShardParticles { get; set; }

    public bool IsBroken { get; private set; }

    public override void _Ready()
    {
        if (WindowMesh == null)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(2.0f, 1.6f, 0.05f) },
                Name = "DefaultWindowMesh",
            };
            AddChild(mesh);
            WindowMesh = mesh;
        }

        if (WindowCollision == null)
        {
            var col = new CollisionShape3D
            {
                Shape = new BoxShape3D { Size = new Vector3(2.0f, 1.6f, 0.05f) },
                Name = "DefaultWindowCollision",
            };
            AddChild(col);
            WindowCollision = col;
        }

        if (ShardParticles != null)
            ShardParticles.Emitting = false;
    }

    public override void _ExitTree()
    {
        // StaticBody3D has no body signals by default; explicit only.
    }

    /// <summary>
    /// Public entry point: forcibly break the window. Safe to call multiple
    /// times — subsequent calls are no-ops.
    /// </summary>
    public void BreakWindow()
    {
        if (IsBroken) return;
        IsBroken = true;

        if (WindowMesh != null)
            WindowMesh.Visible = false;

        // Disable collision so subsequent physics can pass through.
        if (WindowCollision != null)
            WindowCollision.Disabled = true;

        if (ShardParticles != null)
        {
            ShardParticles.Emitting = true;
            // Let the particle node free itself after emission so we don't keep
            // an invisible GPU particle system alive in the scene.
            var t = GetTree().CreateTimer(3.0);
            t.Timeout += () =>
            {
                if (IsInstanceValid(ShardParticles))
                    ShardParticles.QueueFree();
            };
        }

        // Random shatter SFX (null-safe via AudioAssets.RandomGlassShatter()).
        AudioAssets.PlayOneShot3D(AudioAssets.RandomGlassShatter(), this, GlobalPosition, "SFX");

        // Notify the Listener.
        foreach (Node node in GetTree().GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                listener.HearNoise(new ListenerSoundEvent(GlobalPosition, NoiseTier, SoundKind.Environment, this));
        }

        GD.Print($"BreakableWindow: shattered at {GlobalPosition}.");
    }

    /// <summary>
    /// Returns true if <paramref name="body"/> is a RigidBody3D whose linear
    /// velocity magnitude is at least <see cref="BreakVelocity"/>.
    /// </summary>
    private bool IsHighSpeedImpact(Node3D body)
    {
        if (body is RigidBody3D rb)
            return rb.LinearVelocity.Length() >= BreakVelocity;
        return false;
    }

    /// <summary>
    /// Convenience for callers that want to gate BreakWindow() on a relative
    /// velocity value (e.g. from a player melee hit).
    /// </summary>
    public bool ShouldBreakFromImpact(float relativeSpeed)
    {
        return relativeSpeed >= BreakVelocity;
    }

    /// <summary>
    /// Listen-on-collision helper. BreakableWindow extends StaticBody3D which
    /// does not emit BodyEntered by default — the simplest hook is to call
    /// <see cref="TryBreakFromContact"/> from the world builder script that
    /// owns the window's parent. We expose it here for testability.
    /// </summary>
    public void TryBreakFromContact(Node3D body)
    {
        if (IsBroken) return;
        if (IsHighSpeedImpact(body))
            BreakWindow();
    }
}
