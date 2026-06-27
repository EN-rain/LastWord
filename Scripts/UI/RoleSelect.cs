using Godot;
using LastWord.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// Lobby role-selection UI (§5).
/// Enforces no-duplicate roles and offers a "No Role" option when there are
/// more than 3 players.
/// </summary>
public partial class RoleSelect : Control
{
	[Signal] public delegate void RoleSelectedEventHandler(int role);
	[Signal] public delegate void RoleConfirmedEventHandler(int role);

	[Export] public NodePath NoRoleButtonPath;
	[Export] public NodePath LoudButtonPath;
	[Export] public NodePath StaticButtonPath;
	[Export] public NodePath MuteButtonPath;
	[Export] public NodePath ArchivistButtonPath;
	[Export] public NodePath WitnessButtonPath;
	[Export] public NodePath ConfirmButtonPath;
	[Export] public NodePath StatusLabelPath;

	private readonly Dictionary<PlayerRole, Button> _buttons = new();
	private PlayerRole _selectedRole = PlayerRole.None;
	private readonly HashSet<PlayerRole> _takenRoles = new();

	public PlayerRole SelectedRole => _selectedRole;

	public override void _Ready()
	{
		UiSounds.WireButtonsInNode(this);
		RegisterButton(PlayerRole.None, NoRoleButtonPath);
		RegisterButton(PlayerRole.Loud, LoudButtonPath);
		RegisterButton(PlayerRole.Static, StaticButtonPath);
		RegisterButton(PlayerRole.Mute, MuteButtonPath);
		RegisterButton(PlayerRole.Archivist, ArchivistButtonPath);
		RegisterButton(PlayerRole.Witness, WitnessButtonPath);

		Button confirm = GetNodeOrNull<Button>(ConfirmButtonPath);
		if (confirm != null)
			confirm.Pressed += OnConfirm;

		RefreshUI();
	}

	private void RegisterButton(PlayerRole role, NodePath path)
	{
		Button btn = GetNodeOrNull<Button>(path);
		if (btn == null)
			return;

		_buttons[role] = btn;
		btn.Pressed += () => SelectRole(role);
	}

	public void SetTakenRoles(IEnumerable<PlayerRole> roles)
	{
		_takenRoles.Clear();
		foreach (PlayerRole role in roles)
			_takenRoles.Add(role);
		RefreshUI();
	}

	public void MarkRoleTaken(PlayerRole role, bool taken)
	{
		if (taken)
			_takenRoles.Add(role);
		else
			_takenRoles.Remove(role);
		RefreshUI();
	}

	private void SelectRole(PlayerRole role)
	{
		_selectedRole = role;
		EmitSignal(SignalName.RoleSelected, (int)role);
		RefreshUI();
	}

	private void OnConfirm()
	{
		EmitSignal(SignalName.RoleConfirmed, (int)_selectedRole);
	}

	private void RefreshUI()
	{
		foreach (var kvp in _buttons)
		{
			PlayerRole role = kvp.Key;
			Button btn = kvp.Value;
			bool taken = _takenRoles.Contains(role) && role != _selectedRole;
			btn.Disabled = taken;
			btn.ButtonPressed = role == _selectedRole;
		}

		Label status = GetNodeOrNull<Label>(StatusLabelPath);
		if (status != null)
		{
			if (_selectedRole == PlayerRole.None)
				status.Text = "No role selected.";
			else if (_takenRoles.Contains(_selectedRole))
				status.Text = $"{_selectedRole} is already taken.";
			else
				status.Text = $"Selected: {_selectedRole}";
		}
	}
}
