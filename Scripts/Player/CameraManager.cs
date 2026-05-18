using Godot;
using System;

public partial class CameraManager : Node3D
{
    [Export] public NodePath TargetPath;
    [Export] public NodePath CameraPivotPath = "Pivot";
    [Export] public NodePath CameraTransformPath = "Pivot/MainCamera";
    
    // In Godot, physics layers are stored as bitmasks (uint)
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayers = 1;

    private Node3D _targetTransform;
    private Node3D _cameraPivot;
    private Camera3D _cameraTransform;

    private Vector3 _cameraFollowVelocity = Vector3.Zero;
    private Vector3 _cameraVectorPosition;

    private float _defaultPosition;
    private Vector3 _followOffset;

    [ExportGroup("Collision")]
    [Export] public float CameraCollisionOffset = 0.2f;
    [Export] public float CameraCollisionRadius = 0.2f;
    [Export] public float MinimumCollisionOffset = 0.2f;
    
    [ExportGroup("Movement")]
    [Export] public float CameraFollowSpeed = 10f; // Adjusted for Lerp

    [ExportGroup("Sensitivity")]
    [Export] public float CameraSensitivity = 0.5f;
    [Export] public float CameraLookSpeed = 1f;
    [Export] public float CameraPivotSpeed = 1f;

    public float PivotAngle;
    public float LookAngle;

    [Export] public float MinimumPivotAngle = -35f;
    [Export] public float MaximumPivotAngle = 35f;

    [ExportGroup("Scroll Zoom")]
    [Export] public float ZoomSpeed = 2f;
    // In Godot, cameras look down -Z, so positive Z is behind the pivot
    [Export] public float MinZoomDistance = 1f;  // closest
    [Export] public float MaxZoomDistance = 5f;  // furthest
    private float _targetZoom;

    // Inputs from mouse
    private float _cameraInputX;
    private float _cameraInputY;

    public override void _Ready()
    {
        AddToGroup("CameraManager");

        if (TargetPath != null) _targetTransform = GetNodeOrNull<Node3D>(TargetPath);

        // If no target is set, default to the parent node (the Player)
        if (_targetTransform == null) _targetTransform = GetParent<Node3D>();

        if (CameraPivotPath != null) _cameraPivot = GetNodeOrNull<Node3D>(CameraPivotPath);
        if (CameraTransformPath != null) _cameraTransform = GetNodeOrNull<Camera3D>(CameraTransformPath);

        if (_cameraTransform != null)
        {
            _defaultPosition = _cameraTransform.Position.Z;
            _targetZoom = _defaultPosition;
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        // Load saved mouse sensitivity from settings.cfg
        var cfg = new ConfigFile();
        if (cfg.Load("user://settings.cfg") == Error.Ok)
        {
            CameraSensitivity = (float)cfg.GetValue("controls", "mouse_sens", 0.2f);
        }

        // Store the initial local position (e.g. Y=1.5) to use as an offset from the player's feet
        _followOffset = Position;
        
        // Detach from parent so it can follow smoothly in world space
        TopLevel = true;
        
        // Ensure this script runs AFTER the player has moved in the physics step to eliminate jitter
        ProcessPhysicsPriority = 100;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _cameraInputX += mouseMotion.Relative.X;
            _cameraInputY += mouseMotion.Relative.Y;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
            {
                _targetZoom -= ZoomSpeed * 0.5f;
                _targetZoom = Mathf.Clamp(_targetZoom, MinZoomDistance, MaxZoomDistance);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
            {
                _targetZoom += ZoomSpeed * 0.5f;
                _targetZoom = Mathf.Clamp(_targetZoom, MinZoomDistance, MaxZoomDistance);
            }

            // Re-capture mouse if user clicks back into the game window
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed
                && Input.MouseMode != Input.MouseModeEnum.Captured)
            {
                // Only recapture if the pause menu is NOT open
                if (!PauseMenu.IsOpen)
                    Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleAllCameraMovement((float)delta);
    }

    public void HandleAllCameraMovement(float delta)
    {
        FollowPlayer(delta);
        RotateCamera(delta);
        HandleScrollZoom(delta);
        HandleCameraCollision(delta);
        
        // Reset input after processing
        _cameraInputX = 0;
        _cameraInputY = 0;
    }

    public void FollowPlayer(float delta)
    {
        if (_targetTransform == null) return;

        // Smooth follow logic translated to Godot
        Vector3 targetPosition = GlobalPosition.Lerp(_targetTransform.GlobalPosition + _followOffset, CameraFollowSpeed * delta);
        GlobalPosition = targetPosition;
    }

    public void RotateCamera(float delta)
    {
        LookAngle -= (_cameraInputX * CameraLookSpeed * CameraSensitivity);
        PivotAngle -= (_cameraInputY * CameraPivotSpeed * CameraSensitivity);
        PivotAngle = Mathf.Clamp(PivotAngle, MinimumPivotAngle, MaximumPivotAngle);

        // Rotate CameraManager around Y
        Vector3 rotation = Vector3.Zero;
        rotation.Y = Mathf.DegToRad(LookAngle);
        Transform3D t = GlobalTransform;
        t.Basis = Basis.FromEuler(rotation);
        GlobalTransform = t;

        // Rotate Pivot around X
        if (_cameraPivot != null)
        {
            Vector3 pivotRot = Vector3.Zero;
            pivotRot.X = Mathf.DegToRad(PivotAngle);
            _cameraPivot.Rotation = pivotRot;
        }
    }

    private void HandleScrollZoom(float delta)
    {
        _defaultPosition = Mathf.Lerp(_defaultPosition, _targetZoom, delta * 8f);
    }

    private void HandleCameraCollision(float delta)
    {
        if (_cameraTransform == null || _cameraPivot == null) return;

        float targetPosition = _defaultPosition;
        
        var spaceState = GetWorld3D().DirectSpaceState;
        
        // Direction from pivot to camera
        Vector3 direction = _cameraTransform.GlobalPosition - _cameraPivot.GlobalPosition;
        float directionLength = direction.Length();
        if (directionLength > 0.001f) direction /= directionLength;

        // Perform raycast
        var query = PhysicsRayQueryParameters3D.Create(_cameraPivot.GlobalPosition, _cameraPivot.GlobalPosition + (direction * Mathf.Abs(targetPosition)));
        query.CollisionMask = CollisionLayers;
        
        // Exclude the player's collision shape to prevent the camera from colliding with the player itself
        if (_targetTransform is CollisionObject3D playerCol)
        {
            query.Exclude = new Godot.Collections.Array<Rid> { playerCol.GetRid() };
        }

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            Vector3 hitPoint = (Vector3)result["position"];
            float distance = _cameraPivot.GlobalPosition.DistanceTo(hitPoint);
            targetPosition = distance - CameraCollisionOffset;
        }

        if (Mathf.Abs(targetPosition) < MinimumCollisionOffset)
        {
            targetPosition = MinimumCollisionOffset;
        }

        // Apply smoothed position using Godot Lerp
        _cameraVectorPosition = _cameraTransform.Position;
        _cameraVectorPosition.Z = Mathf.Lerp(_cameraTransform.Position.Z, targetPosition, delta / 0.2f);
        _cameraTransform.Position = _cameraVectorPosition;
    }
}
