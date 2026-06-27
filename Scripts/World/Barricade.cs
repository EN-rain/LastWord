using Godot;
using LastWord;

namespace LastWord.World;

/// <summary>
/// Heavy door barricade (§6.5 Clock Tower, §10.2 furniture). Players hold interact
/// for <see cref="BarricadeTime"/> seconds to barricade the door. The Listener takes
/// <see cref="BreakTime"/> seconds to break through.
/// </summary>
public partial class Barricade : StaticBody3D
{
    [Export] public float BarricadeTime = 3.0f;
    [Export] public float BreakTime = 12.0f;
    [Export] public bool StartsBarricaded = false;
    [Export] public Area3D InteractionTrigger;
    [Export] public float InteractionRadius = 2.0f;

    private bool _isBarricaded;
    private double _holdTimer;
    private PlayerController _holder;
    private SceneTreeTimer _breakTimer;

    public bool IsBarricaded => _isBarricaded;

    public override void _Ready()
    {
        _isBarricaded = StartsBarricaded;
        InteractionTrigger ??= CreateDefaultTrigger();
        InteractionTrigger.BodyEntered += OnBodyEntered;
        InteractionTrigger.BodyExited += OnBodyExited;
    }

    public override void _ExitTree()
    {
        if (InteractionTrigger != null)
        {
            InteractionTrigger.BodyEntered -= OnBodyEntered;
            InteractionTrigger.BodyExited -= OnBodyExited;
        }
    }

    public override void _Process(double delta)
    {
        if (_isBarricaded || _holder == null || _holder.IsDead) return;

        if (_holder.IsInteracting)
        {
            _holdTimer += delta;
            if (_holdTimer >= BarricadeTime)
            {
                SetBarricaded(true);
                _holdTimer = 0.0;
            }
        }
        else
        {
            _holdTimer = 0.0;
        }
    }

    public void SetHolder(PlayerController player)
    {
        _holder = player;
        _holdTimer = 0.0;
    }

    public void ClearHolder(PlayerController player)
    {
        if (_holder == player)
        {
            _holder = null;
            _holdTimer = 0.0;
        }
    }

    public void SetBarricaded(bool barricaded)
    {
        _isBarricaded = barricaded;
        string path = _isBarricaded ? AudioAssets.Impact01 : AudioAssets.DoorOpen01;
        AudioAssets.PlayOneShot3D(path, this, GlobalPosition, "SFX");
        GD.Print($"Barricade: {(_isBarricaded ? "barricaded" : "open")}.");
    }

    public void StartBreak()
    {
        if (!_isBarricaded) return;
        if (_breakTimer != null) return;
        // ListenerAI calls this and waits BreakTime before passing.
        AudioAssets.PlayOneShot3D(AudioAssets.Impact01, this, GlobalPosition, "SFX");
        GD.Print($"Barricade: breaking through ({BreakTime}s).");
        _breakTimer = GetTree().CreateTimer(BreakTime);
        _breakTimer.Timeout += FinishBreak;
    }

    public void FinishBreak()
    {
        SetBarricaded(false);
        _breakTimer = null;
    }

    private Area3D CreateDefaultTrigger()
    {
        var area = new Area3D { Name = "InteractionTrigger" };
        var collisionShape = new CollisionShape3D { Name = "CollisionShape3D" };
        var shape = new SphereShape3D { Radius = InteractionRadius };
        collisionShape.Shape = shape;
        area.AddChild(collisionShape);
        AddChild(area);
        return area;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController player && !player.IsDead)
            SetHolder(player);
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController player)
            ClearHolder(player);
    }
}
