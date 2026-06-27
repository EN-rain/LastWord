using Godot;

public partial class TokenVisual : Node3D
{
	private Node3D _targetHolder;
	[Export] public float FloatHeight = 1.5f;
	[Export] public float SpinSpeed = 2.0f; // Radians per second

	public override void _Ready()
	{
		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.TokenTransferred += OnTokenTransferred;
		}
		Visible = false;
	}

	public override void _ExitTree()
	{
		if (VoiceManager.Instance != null)
		{
			VoiceManager.Instance.TokenTransferred -= OnTokenTransferred;
		}
	}

	private void OnTokenTransferred(Node3D newHolder)
	{
		_targetHolder = newHolder;
		Visible = _targetHolder != null;
	}

	public override void _Process(double delta)
	{
		if (_targetHolder != null && IsInstanceValid(_targetHolder))
		{
			GlobalPosition = _targetHolder.GlobalPosition + new Vector3(0, FloatHeight, 0);
			RotateY(SpinSpeed * (float)delta);
		}
	}
}
