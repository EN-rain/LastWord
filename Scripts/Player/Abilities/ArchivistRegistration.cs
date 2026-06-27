using Godot;

/// <summary>
/// Archivist role passive (§5.Archivist): silent note registration.
/// Allows the Archivist to register a word at the Registration Board
/// by holding E for 5 seconds without speaking.
/// </summary>
public partial class ArchivistRegistration : Node3D
{
	[Export] public float HoldTime = 5.0f;
	[Export] public string InteractAction = "interact";

	private float _holdTimer = 0f;
	private bool _isHolding = false;
	private RegistrationBoard _currentBoard = null;

	public override void _Process(double delta)
	{
		if (GetParent() is not PlayerController player)
			return;

		RoleData role = player.GetNodeOrNull<RoleData>("RoleData");
		if (role == null || !role.IsArchivist)
			return;

		if (Input.IsActionPressed(InteractAction) && _currentBoard != null)
		{
			_isHolding = true;
			_holdTimer += (float)delta;
			_currentBoard.SetProgress(_holdTimer / HoldTime);

			if (_holdTimer >= HoldTime)
			{
				_holdTimer = 0f;
				_currentBoard.RegisterSilently(player);
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
