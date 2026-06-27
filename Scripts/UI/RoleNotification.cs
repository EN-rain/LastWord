using Godot;

/// <summary>
/// HUD notification for role-related events (§5), e.g. "F stun lost" on death.
/// </summary>
public partial class RoleNotification : Control
{
	[Export] public float DisplayDuration = 3.0f;
	[Export] public NodePath LabelPath;

	private Label _label;
	private float _timer = 0f;

	public override void _Ready()
	{
		_label = GetNodeOrNull<Label>(LabelPath);
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (_timer > 0f)
		{
			_timer -= (float)delta;
			if (_timer <= 0f)
				Visible = false;
		}
	}

	public void Show(string message)
	{
		if (_label != null)
			_label.Text = message;
		Visible = true;
		_timer = DisplayDuration;
	}

	public static void ShowFor(PlayerController player, string message)
	{
		if (player == null)
			return;

		RoleNotification notifier = player.GetNodeOrNull<RoleNotification>("RoleNotification");
		notifier?.Show(message);
	}
}
