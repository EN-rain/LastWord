using Godot;

/// <summary>
/// Radial gesture wheel overlay. Hold the configured MMB action for the hold
/// threshold to open the wheel; release outside the deadzone to select one of
/// eight gestures and emit <see cref="GestureSelected"/>.
/// </summary>
public partial class GestureWheel : CanvasLayer
{
	[Signal] public delegate void GestureSelectedEventHandler(GestureId gesture);

	[Export] public float HoldThresholdSeconds = 0.3f;
	[Export] public float DeadzoneRadius = 20f;
	[Export] public float WheelRadius = 120f;
	[Export] public int SegmentCount = 8;

	private Control _wheelRoot;
	private bool _isHolding;
	private bool _isOpen;
	private float _holdTime;
	private Vector2 _center;

	public bool IsOpen => _isOpen;

	public override void _Ready()
	{
		Layer = 100;
		_wheelRoot = GetNodeOrNull<Control>("WheelRoot");
		if (_wheelRoot == null)
		{
			_wheelRoot = new Control
			{
				Name = "WheelRoot",
				AnchorsPreset = (int)Control.LayoutPreset.FullRect,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			AddChild(_wheelRoot);
		}

		_wheelRoot.Hide();
		_wheelRoot.Resized += OnWheelResized;
		_wheelRoot.Draw += OnWheelDraw;
	}

	public override void _ExitTree()
	{
		if (_wheelRoot != null)
		{
			_wheelRoot.Resized -= OnWheelResized;
			_wheelRoot.Draw -= OnWheelDraw;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("radial_wheel_mmb"))
		{
			_isHolding = true;
			_holdTime = 0f;
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionReleased("radial_wheel_mmb"))
		{
			if (_isOpen)
			{
				TrySelectGesture();
			}

			CloseWheel();
			_isHolding = false;
			_holdTime = 0f;
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Process(double delta)
	{
		if (!_isHolding)
			return;

		_holdTime += (float)delta;
		if (!_isOpen && _holdTime >= HoldThresholdSeconds)
		{
			OpenWheel();
		}

		if (_isOpen && _wheelRoot != null)
		{
			_wheelRoot.QueueRedraw();
		}
	}

	private void OpenWheel()
	{
		_isOpen = true;
		UpdateCenter();
		_wheelRoot?.Show();
		_wheelRoot?.QueueRedraw();
	}

	private void CloseWheel()
	{
		_isOpen = false;
		_wheelRoot?.Hide();
	}

	private void TrySelectGesture()
	{
		GestureId? gesture = GetHoveredGesture();
		if (gesture.HasValue)
			EmitSignal(SignalName.GestureSelected, (int)gesture.Value);
	}

	/// <summary>
	/// Returns the gesture currently highlighted by the mouse, or null if the
	/// cursor is inside the deadzone.
	/// </summary>
	public GestureId? GetHoveredGesture()
	{
		if (!_isOpen)
			return null;

		Vector2 mouse = _wheelRoot.GetGlobalMousePosition();
		Vector2 delta = mouse - _center;
		float distance = delta.Length();
		if (distance <= DeadzoneRadius)
			return null;

		return GetGestureForDirection(delta);
	}

	/// <summary>
	/// Maps a screen-space direction to one of the eight gesture segments.
	/// Segment 0 is centered at the top (12 o'clock) and indices increase clockwise.
	/// </summary>
	public GestureId? GetGestureForDirection(Vector2 direction)
	{
		if (direction == Vector2.Zero)
			return null;

		float angle = Mathf.Atan2(direction.Y, direction.X);
		float adjusted = angle + Mathf.Pi / 2f;
		int segment = Mathf.RoundToInt(adjusted / (Mathf.Pi * 2f / SegmentCount));
		segment = ((segment % SegmentCount) + SegmentCount) % SegmentCount;

		if (segment < 0 || segment > (int)GestureId.Stop)
			return null;

		return (GestureId)segment;
	}

	private void UpdateCenter()
	{
		if (_wheelRoot == null)
			return;

		Rect2 rect = _wheelRoot.GetGlobalRect();
		_center = rect.Size / 2f;
	}

	private void OnWheelResized()
	{
		UpdateCenter();
		_wheelRoot?.QueueRedraw();
	}

	private void OnWheelDraw()
	{
		if (_wheelRoot == null)
			return;

		_wheelRoot.DrawCircle(_center, WheelRadius, new Color(0f, 0f, 0f, 0.75f));
		_wheelRoot.DrawCircle(_center, DeadzoneRadius, new Color(0.1f, 0.1f, 0.1f, 0.9f));

		for (int i = 0; i < SegmentCount; i++)
		{
			float startAngle = Mathf.Pi / 2f + (i - 0.5f) * (Mathf.Pi * 2f / SegmentCount);
			float endAngle = startAngle + Mathf.Pi * 2f / SegmentCount;
			DrawSegment(i, startAngle, endAngle);
		}
	}

	private void DrawSegment(int index, float startAngle, float endAngle)
	{
		GestureId? hovered = GetHoveredGesture();
		bool isHovered = hovered.HasValue && (int)hovered.Value == index;
		Color color = isHovered
			? new Color(0.9f, 0.9f, 0.2f, 0.85f)
			: new Color(0.7f, 0.7f, 0.7f, 0.5f);

		Vector2[] points = new Vector2[4];
		points[0] = _center;
		points[1] = _center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * WheelRadius;
		points[2] = _center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * WheelRadius;
		points[3] = _center;
		_wheelRoot.DrawColoredPolygon(points, color);

		float midAngle = (startAngle + endAngle) / 2f;
		Vector2 labelPos = _center + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * (WheelRadius * 0.65f);
		string label = ((GestureId)index).ToString();
		_wheelRoot.DrawString(ThemeDB.FallbackFont, labelPos, label, HorizontalAlignment.Center, -1, 14, Colors.White);
	}
}
