using Godot;

public partial class PauseMenu : CanvasLayer
{
    // --- UI Node Exports ---
    [Export] public NodePath OverlayPath;
    [Export] public NodePath MenuPanelPath;
    [Export] public NodePath ResumeButtonPath;
    [Export] public NodePath SettingsButtonPath;
    [Export] public NodePath QuitButtonPath;
    [Export] public NodePath SettingsMenuPath;

    [Export] public int CanvasLayerIndex = 10;

    // --- Public state flag (read by PlayerController) ---
    public static bool IsOpen { get; private set; } = false;

    // --- Private Node references ---
    private Control _menuPanel;
    private ColorRect _overlay;
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _quitButton;
    private Control _settingsMenu;

    public override void _Ready()
    {
        // GD.Print("PauseMenu: Initializing dynamic overlay...");

        _overlay = GetNodeOrNull<ColorRect>(OverlayPath);
        _menuPanel = GetNodeOrNull<Control>(MenuPanelPath);
        _resumeButton = GetNodeOrNull<Button>(ResumeButtonPath);
        _settingsButton = GetNodeOrNull<Button>(SettingsButtonPath);
        _quitButton = GetNodeOrNull<Button>(QuitButtonPath);
        _settingsMenu = GetNodeOrNull<Control>(SettingsMenuPath);

        if (_resumeButton != null) _resumeButton.Pressed += OnResumePressed;
        if (_settingsButton != null) _settingsButton.Pressed += OnSettingsPressed;
        if (_quitButton != null) _quitButton.Pressed += OnQuitPressed;

        if (_menuPanel != null) _menuPanel.Visible = false;
        if (_overlay != null) _overlay.Visible = false;
        if (_settingsMenu != null) 
        {
            _settingsMenu.Visible = false;
            if (_settingsMenu is SettingsMenu menu)
            {
                menu.IsSimplifiedMode = true;
            }
        }

        Layer = CanvasLayerIndex;
    }

    public override void _ExitTree()
    {
        if (_resumeButton != null) _resumeButton.Pressed -= OnResumePressed;
        if (_settingsButton != null) _settingsButton.Pressed -= OnSettingsPressed;
        if (_quitButton != null) _quitButton.Pressed -= OnQuitPressed;
        IsOpen = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            // If settings is open, close it first and return to pause menu
            if (_settingsMenu != null && _settingsMenu.Visible)
            {
                _settingsMenu.Visible = false;
                if (_menuPanel != null) _menuPanel.Visible = true;
            }
            else
            {
                ToggleMenu();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    private void ToggleMenu()
    {
        IsOpen = !IsOpen;
        if (_menuPanel != null) _menuPanel.Visible = IsOpen;
        if (_overlay != null) _overlay.Visible = IsOpen;

        if (!IsOpen)
        {
            if (_settingsMenu != null) _settingsMenu.Visible = false;
        }

        // Cursor management
        Input.MouseMode = IsOpen
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;
    }

    private void OnResumePressed() => ToggleMenu();

    private void OnSettingsPressed()
    {
        if (_settingsMenu != null)
        {
            if (_menuPanel != null) _menuPanel.Visible = false; // Hide main pause menu buttons
            _settingsMenu.Visible = true;
            if (_settingsMenu is SettingsMenu menu)
            {
                menu.LoadSettings();
            }
        }
    }

    private void OnQuitPressed()
    {
        IsOpen = false;
        GetTree().Quit();
    }
}
