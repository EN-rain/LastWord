using Godot;

/// <summary>
/// Mute role ability (§5.Mute): Silent Drop at the Registration Board.
/// Hold E on a Registration Board for 2 seconds to deposit the currently
/// held note without speaking.
/// </summary>
public partial class MuteSilentDrop : Node3D
{
	[Export] public float HoldTime = 2.0f;
	[Export] public string InteractAction = "interact";

	private float _holdTimer = 0f;
	private bool _isHolding = false;
	private RegistrationBoard _currentBoard = null;

	public override void _Process(double delta)
	{
		if (GetParent() is not PlayerController player)
			return;

		RoleData role = player.GetNodeOrNull<RoleData>("RoleData");
		if (role == null || !role.IsMute)
			return;

		if (Input.IsActionPressed(InteractAction) && _currentBoard != null)
		{
			_isHolding = true;
			_holdTimer += (float)delta;
			_currentBoard.SetProgress(_holdTimer / HoldTime);

			if (_holdTimer >= HoldTime)
			{
				_holdTimer = 0f;
				_currentBoard.SilentDrop(player);
			}
		}
		else
		{
			if (_isHolding && _currentBoard != null)
				_currentBoard.SetProgress(0f);
			_isHolding = false;
			_holdTimer = 0f;
		}
	}

	public void OnEnterBoard(RegistrationBoard board)
	{
		_currentBoard = board;
		_holdTimer = 0f;
	}

	public void OnExitBoard()
	{
		_currentBoard = null;
		_holdTimer = 0f;
	}
}
