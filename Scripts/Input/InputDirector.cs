using System;
using Godot;
using PictoMino.View;

namespace PictoMino.Input;

/// <summary>
/// 输入设备类型。
/// </summary>
public enum InputDeviceType
{
	Mouse,
	Gamepad
}

/// <summary>
/// 输入管理器。自动检测并切换鼠标/手柄输入策略。
/// </summary>
public partial class InputDirector : Node
{
	private BoardView? _boardView;

	/// <summary>棋盘行数（用于初始化 GamepadStrategy）</summary>
	[Export] public int BoardRows { get; set; } = 10;

	/// <summary>棋盘列数（用于初始化 GamepadStrategy）</summary>
	[Export] public int BoardCols { get; set; } = 10;

	private MouseStrategy? _mouseStrategy;
	private GamepadStrategy? _gamepadStrategy;
	private IInputStrategy? _activeStrategy;
	private InputDeviceType _currentDevice = InputDeviceType.Mouse;

	/// <summary>
	/// 当前活跃的输入设备类型。
	/// </summary>
	public InputDeviceType CurrentDevice => _currentDevice;

	/// <summary>
	/// 当输入设备切换时触发。
	/// </summary>
	public event Action<InputDeviceType>? OnDeviceChanged;

	/// <summary>
	/// 当 Ghost 位置变化时触发。参数为棋盘坐标 (col, row)。
	/// </summary>
	public event Action<Vector2I>? OnGhostPositionChanged;

	/// <summary>
	/// 当交互操作触发时触发。参数为棋盘坐标 (col, row)。
	/// </summary>
	public event Action<Vector2I>? OnInteract;

	/// <summary>
	/// 当取消操作触发时触发。
	/// </summary>
	public event Action? OnCancel;

	/// <summary>
	/// 当顺时针旋转时触发。
	/// </summary>
	public event Action? OnRotateClockwise;

	/// <summary>
	/// 当逆时针旋转时触发。
	/// </summary>
	public event Action? OnRotateCounterClockwise;

	/// <summary>
	/// 当选择下一个形状时触发。
	/// </summary>
	public event Action? OnSelectNextShape;

	/// <summary>
	/// 当选择上一个形状时触发。
	/// </summary>
	public event Action? OnSelectPreviousShape;

	public override void _Ready()
	{
		_boardView = GetNodeOrNull<BoardView>("%BoardView");
		if (_boardView == null)
		{
			GD.PrintErr("InputDirector: BoardView not found.");
			return;
		}

		InitializeStrategies();
		
		// 根据上次使用的输入设备初始化
		var session = GameSession.Instance;
		if (session != null && session.LastInputWasGamepad)
		{
			SwitchToDevice(InputDeviceType.Gamepad);
			_gamepadStrategy?.SetCursorPosition(new Vector2I(BoardCols / 2, BoardRows / 2));
			OnGhostPositionChanged?.Invoke(new Vector2I(BoardCols / 2, BoardRows / 2));
		}
		else
		{
			SwitchToDevice(InputDeviceType.Mouse);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// 检测输入设备切换
		InputDeviceType detectedDevice = DetectDeviceFromEvent(@event);
		if (detectedDevice != _currentDevice)
		{
			SwitchToDevice(detectedDevice);
		}

		// 让当前策略处理输入
		_activeStrategy?.HandleInput(@event);
	}

	public override void _Process(double delta)
	{
		_activeStrategy?.Process(delta);
	}

	/// <summary>
	/// 获取当前 Ghost 的棋盘位置。
	/// </summary>
	public Vector2I? GetGhostGridPosition()
	{
		return _activeStrategy?.GetGhostGridPosition();
	}

	/// <summary>
	/// 重置光标到棋盘中心。
	/// </summary>
	public void ResetCursorToCenter()
	{
		var centerPos = new Vector2I(BoardCols / 2, BoardRows / 2);
		_gamepadStrategy?.SetCursorPosition(centerPos);
		
		if (_currentDevice == InputDeviceType.Gamepad)
		{
			OnGhostPositionChanged?.Invoke(centerPos);
		}
	}

	/// <summary>
	/// 强制切换到指定输入设备。
	/// </summary>
	public void SwitchToDevice(InputDeviceType device)
	{
		if (_currentDevice == device && _activeStrategy != null) return;

		// 切换前保存当前位置
		Vector2I? currentPos = _activeStrategy?.GetGhostGridPosition();

		_activeStrategy?.OnDeactivate();

		_currentDevice = device;
		_activeStrategy = device switch
		{
			InputDeviceType.Mouse => _mouseStrategy,
			InputDeviceType.Gamepad => _gamepadStrategy,
			_ => _mouseStrategy
		};

		// 保存输入设备类型到 GameSession
		var session = GameSession.Instance;
		if (session != null)
		{
			session.LastInputWasGamepad = (device == InputDeviceType.Gamepad);
		}

		// 切换到手柄时,同步光标位置或使用棋盘中心
		if (device == InputDeviceType.Gamepad && _gamepadStrategy != null)
		{
			if (currentPos.HasValue && currentPos.Value.X >= 0 && currentPos.Value.Y >= 0 
			    && currentPos.Value.X < BoardCols && currentPos.Value.Y < BoardRows)
			{
				_gamepadStrategy.SetCursorPosition(currentPos.Value);
			}
			else
			{
				_gamepadStrategy.SetCursorPosition(new Vector2I(BoardCols / 2, BoardRows / 2));
			}
		}

		_activeStrategy?.OnActivate();
		OnDeviceChanged?.Invoke(device);

		GD.Print($"InputDirector: Switched to {device} input.");
	}

	/// <summary>
	/// 初始化输入策略。
	/// </summary>
	private void InitializeStrategies()
	{
		if (_boardView == null) return;

		// 初始化鼠标策略
		_mouseStrategy = new MouseStrategy(_boardView, this);
		_mouseStrategy.OnGridPositionChanged += pos => OnGhostPositionChanged?.Invoke(pos);
		_mouseStrategy.OnInteract += pos => OnInteract?.Invoke(pos);
		_mouseStrategy.OnCancel += () => OnCancel?.Invoke();
		_mouseStrategy.OnRotateClockwise += () => OnRotateClockwise?.Invoke();
		_mouseStrategy.OnRotateCounterClockwise += () => OnRotateCounterClockwise?.Invoke();

		// 初始化手柄策略
		_gamepadStrategy = new GamepadStrategy(BoardRows, BoardCols);
		_gamepadStrategy.OnGridPositionChanged += pos => OnGhostPositionChanged?.Invoke(pos);
		_gamepadStrategy.OnInteract += pos => OnInteract?.Invoke(pos);
		_gamepadStrategy.OnCancel += () => OnCancel?.Invoke();
		_gamepadStrategy.OnRotateClockwise += () => OnRotateClockwise?.Invoke();
		_gamepadStrategy.OnRotateCounterClockwise += () => OnRotateCounterClockwise?.Invoke();
		_gamepadStrategy.OnSelectNextShape += () => OnSelectNextShape?.Invoke();
		_gamepadStrategy.OnSelectPreviousShape += () => OnSelectPreviousShape?.Invoke();
	}

	/// <summary>
	/// 根据输入事件检测设备类型。
	/// </summary>
	private static InputDeviceType DetectDeviceFromEvent(InputEvent @event)
	{
		return @event switch
		{
			InputEventMouseMotion => InputDeviceType.Mouse,
			InputEventMouseButton => InputDeviceType.Mouse,
			InputEventKey => InputDeviceType.Gamepad, // 键盘视为手柄输入
			InputEventJoypadButton => InputDeviceType.Gamepad,
			InputEventJoypadMotion => InputDeviceType.Gamepad,
			_ => InputDeviceType.Mouse
		};
	}
}
