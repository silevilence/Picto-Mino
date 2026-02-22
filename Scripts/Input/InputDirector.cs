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

	public override void _Ready()
	{
		_boardView = GetNodeOrNull<BoardView>("%BoardView");
		if (_boardView == null)
		{
			GD.PrintErr("InputDirector: BoardView not found.");
			return;
		}

		InitializeStrategies();
		SwitchToDevice(InputDeviceType.Mouse);
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
	/// 强制切换到指定输入设备。
	/// </summary>
	public void SwitchToDevice(InputDeviceType device)
	{
		if (_currentDevice == device && _activeStrategy != null) return;

		_activeStrategy?.OnDeactivate();

		_currentDevice = device;
		_activeStrategy = device switch
		{
			InputDeviceType.Mouse => _mouseStrategy,
			InputDeviceType.Gamepad => _gamepadStrategy,
			_ => _mouseStrategy
		};

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

		// 初始化手柄策略
		_gamepadStrategy = new GamepadStrategy(BoardRows, BoardCols);
		_gamepadStrategy.OnGridPositionChanged += pos => OnGhostPositionChanged?.Invoke(pos);
		_gamepadStrategy.OnInteract += pos => OnInteract?.Invoke(pos);
		_gamepadStrategy.OnCancel += () => OnCancel?.Invoke();
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
