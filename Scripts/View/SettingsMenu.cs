using System;
using System.Collections.Generic;
using Godot;

namespace PictoMino.View;

/// <summary>
/// 设置菜单界面，支持按键重映射。
/// </summary>
public partial class SettingsMenu : CanvasLayer
{
    private Control? _root;
    private VBoxContainer? _bindingsContainer;
    private ScrollContainer? _scrollContainer;
    private Button? _backButton;
    private List<Control> _focusableControls = new();
    private int _focusedIndex = 0;

    private bool _isWaitingForInput = false;
    private bool _justFinishedBinding = false;
    private string? _pendingAction;
    private Button? _pendingButton;
    private bool _isGamepad = false;

    private static readonly string[] EditableActions = {
        "cursor_up", "cursor_down", "cursor_left", "cursor_right",
        "interact_main", "interact_secondary",
        "rotate_cw", "rotate_ccw"
    };

    private static readonly Dictionary<string, string> ActionDisplayNames = new()
    {
        { "cursor_up", "上移" },
        { "cursor_down", "下移" },
        { "cursor_left", "左移" },
        { "cursor_right", "右移" },
        { "interact_main", "确认/放置" },
        { "interact_secondary", "取消/移除" },
        { "rotate_cw", "顺时针旋转" },
        { "rotate_ccw", "逆时针旋转" }
    };

    public event Action? OnBack;

    public bool IsWaitingForInput => _isWaitingForInput;
    public bool JustFinishedBinding => _justFinishedBinding;

    public void ClearJustFinishedBinding()
    {
        _justFinishedBinding = false;
    }

    public override void _Ready()
    {
        CreateUI();
    }

    public override void _Process(double delta)
    {
        if (_root != null && Visible)
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            if (_root.Size != viewportSize)
            {
                _root.Size = viewportSize;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isWaitingForInput || _pendingAction == null || _pendingButton == null) return;

        bool validInput = false;
        InputEvent? newEvent = null;

        if (_isGamepad)
        {
            if (@event is InputEventJoypadButton joyBtn && joyBtn.Pressed)
            {
                newEvent = joyBtn.Duplicate() as InputEvent;
                validInput = true;
            }
            else if (@event is InputEventJoypadMotion joyMotion && Mathf.Abs(joyMotion.AxisValue) > 0.5f)
            {
                var motionEvent = new InputEventJoypadMotion
                {
                    Axis = joyMotion.Axis,
                    AxisValue = joyMotion.AxisValue > 0 ? 1.0f : -1.0f
                };
                newEvent = motionEvent;
                validInput = true;
            }
        }
        else
        {
            if (@event is InputEventKey key && key.Pressed && !key.Echo)
            {
                newEvent = key.Duplicate() as InputEvent;
                validInput = true;
            }
        }

        if (validInput && newEvent != null)
        {
            RemoveExistingBinding(_pendingAction, _isGamepad);
            InputMap.ActionAddEvent(_pendingAction, newEvent);
            SyncUiActions(_pendingAction);

            _pendingButton.Text = GetEventDisplayName(newEvent);
            _pendingButton.Modulate = (_focusableControls.IndexOf(_pendingButton) == _focusedIndex) 
                ? new Color(1.2f, 1.2f, 0.8f) : Colors.White;

            _isWaitingForInput = false;
            _justFinishedBinding = true;
            _pendingAction = null;
            _pendingButton = null;

            GetViewport().SetInputAsHandled();
        }
    }

    public void NavigateFocus(int direction)
    {
        if (_isWaitingForInput || _focusableControls.Count == 0) return;

        _focusedIndex = (_focusedIndex + direction + _focusableControls.Count) % _focusableControls.Count;
        UpdateFocusVisual();
    }

    private void UpdateFocusVisual()
    {
        for (int i = 0; i < _focusableControls.Count; i++)
        {
            if (_focusableControls[i] is Button btn)
            {
                btn.Modulate = (i == _focusedIndex) ? new Color(1.2f, 1.2f, 0.8f) : Colors.White;
            }
        }

        if (_scrollContainer != null && _focusedIndex >= 0 && _focusedIndex < _focusableControls.Count)
        {
            var control = _focusableControls[_focusedIndex];
            if (control.GetParent()?.GetParent()?.GetParent() == _bindingsContainer)
            {
                var controlTop = control.GlobalPosition.Y;
                var controlBottom = controlTop + control.Size.Y;
                var scrollTop = _scrollContainer.GlobalPosition.Y;
                var scrollBottom = scrollTop + _scrollContainer.Size.Y;

                if (controlBottom > scrollBottom)
                {
                    _scrollContainer.ScrollVertical += (int)(controlBottom - scrollBottom + 10);
                }
                else if (controlTop < scrollTop)
                {
                    _scrollContainer.ScrollVertical -= (int)(scrollTop - controlTop + 10);
                }
            }
        }
    }

    public void ActivateFocusedButton()
    {
        if (_isWaitingForInput) return;

        if (_focusedIndex >= 0 && _focusedIndex < _focusableControls.Count)
        {
            if (_focusableControls[_focusedIndex] is Button btn)
            {
                btn.EmitSignal("pressed");
            }
        }
    }

    public void FocusBackButton()
    {
        if (_backButton != null && _focusableControls.Contains(_backButton))
        {
            _focusedIndex = _focusableControls.IndexOf(_backButton);
            UpdateFocusVisual();
        }
    }

    public void CancelWaiting()
    {
        if (_isWaitingForInput && _pendingButton != null && _pendingAction != null)
        {
            var events = InputMap.ActionGetEvents(_pendingAction);
            InputEvent? currentEvent = null;
            foreach (var ev in events)
            {
                bool isJoypad = ev is InputEventJoypadButton;
                if (isJoypad == _isGamepad)
                {
                    currentEvent = ev;
                    break;
                }
            }
            _pendingButton.Text = currentEvent != null ? GetEventDisplayName(currentEvent) : "未绑定";
            _pendingButton.Modulate = Colors.White;

            _isWaitingForInput = false;
            _pendingAction = null;
            _pendingButton = null;
        }
    }

    public void ShowMenu()
    {
        RefreshBindings();
        Show();
        if (_focusableControls.Count > 0)
        {
            _focusedIndex = 0;
            UpdateFocusVisual();
        }
    }

    private void CreateUI()
    {
        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        var background = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.15f, 1f)
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(background);

        var mainContainer = new MarginContainer();
        mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        mainContainer.AddThemeConstantOverride("margin_left", 40);
        mainContainer.AddThemeConstantOverride("margin_right", 40);
        mainContainer.AddThemeConstantOverride("margin_top", 40);
        mainContainer.AddThemeConstantOverride("margin_bottom", 40);
        _root.AddChild(mainContainer);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 20);
        mainContainer.AddChild(vbox);

        var titleBar = new HBoxContainer();
        vbox.AddChild(titleBar);

        _backButton = new Button
        {
            Text = "← 返回",
            CustomMinimumSize = new Vector2(100, 40),
            FocusMode = Control.FocusModeEnum.None
        };
        _backButton.Pressed += () =>
        {
            CancelWaiting();
            OnBack?.Invoke();
        };
        titleBar.AddChild(_backButton);

        var spacer1 = new Control();
        spacer1.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        titleBar.AddChild(spacer1);

        var titleLabel = new Label { Text = "设置" };
        titleLabel.AddThemeFontSizeOverride("font_size", 36);
        titleBar.AddChild(titleLabel);

        var spacer2 = new Control();
        spacer2.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        titleBar.AddChild(spacer2);

        var placeholder = new Control { CustomMinimumSize = new Vector2(100, 40) };
        titleBar.AddChild(placeholder);

        _scrollContainer = new ScrollContainer();
        _scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _scrollContainer.FollowFocus = false;
        vbox.AddChild(_scrollContainer);

        _bindingsContainer = new VBoxContainer();
        _bindingsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _bindingsContainer.AddThemeConstantOverride("separation", 30);
        _scrollContainer.AddChild(_bindingsContainer);
    }

    private void RefreshBindings()
    {
        if (_bindingsContainer == null) return;

        _focusableControls.Clear();
        foreach (var child in _bindingsContainer.GetChildren())
        {
            child.QueueFree();
        }

        if (_backButton != null)
        {
            _focusableControls.Add(_backButton);
        }

        CreateBindingSection("键盘", false);
        CreateBindingSection("手柄", true);

        SetupFocusNeighbors();
    }

    private void CreateBindingSection(string title, bool isGamepad)
    {
        if (_bindingsContainer == null) return;

        var sectionBox = new VBoxContainer();
        sectionBox.AddThemeConstantOverride("separation", 10);
        _bindingsContainer.AddChild(sectionBox);

        var sectionLabel = new Label { Text = title };
        sectionLabel.AddThemeFontSizeOverride("font_size", 24);
        sectionBox.AddChild(sectionLabel);

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 20);
        grid.AddThemeConstantOverride("v_separation", 10);
        sectionBox.AddChild(grid);

        foreach (var action in EditableActions)
        {
            var actionLabel = new Label
            {
                Text = ActionDisplayNames.GetValueOrDefault(action, action),
                CustomMinimumSize = new Vector2(150, 0)
            };
            grid.AddChild(actionLabel);

            var currentBinding = GetCurrentBinding(action, isGamepad);
            var bindButton = new Button
            {
                Text = currentBinding,
                CustomMinimumSize = new Vector2(180, 40),
                FocusMode = Control.FocusModeEnum.None
            };

            string capturedAction = action;
            bool capturedIsGamepad = isGamepad;
            bindButton.Pressed += () => StartRebind(capturedAction, bindButton, capturedIsGamepad);

            grid.AddChild(bindButton);
            _focusableControls.Add(bindButton);

            var spacer = new Control { CustomMinimumSize = new Vector2(50, 0) };
            grid.AddChild(spacer);
        }
    }

    private void StartRebind(string action, Button button, bool isGamepad)
    {
        _isWaitingForInput = true;
        _pendingAction = action;
        _pendingButton = button;
        _isGamepad = isGamepad;

        button.Text = isGamepad ? "按下手柄按键..." : "按下键盘按键...";
        button.Modulate = new Color(1f, 1f, 0.5f);
    }

    private string GetCurrentBinding(string action, bool isGamepad)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
        {
            bool isJoypadEvent = ev is InputEventJoypadButton;
            if (isJoypadEvent == isGamepad)
            {
                return GetEventDisplayName(ev);
            }
        }
        return "未绑定";
    }

    private static string GetEventDisplayName(InputEvent ev)
    {
        if (ev is InputEventKey key)
        {
            var keycode = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
            return OS.GetKeycodeString(keycode);
        }
        if (ev is InputEventJoypadButton joyBtn)
        {
            return joyBtn.ButtonIndex switch
            {
                JoyButton.A => "A",
                JoyButton.B => "B",
                JoyButton.X => "X",
                JoyButton.Y => "Y",
                JoyButton.LeftShoulder => "LB",
                JoyButton.RightShoulder => "RB",
                JoyButton.LeftStick => "L3",
                JoyButton.RightStick => "R3",
                JoyButton.Back => "Back",
                JoyButton.Start => "Start",
                JoyButton.DpadUp => "D-Up",
                JoyButton.DpadDown => "D-Down",
                JoyButton.DpadLeft => "D-Left",
                JoyButton.DpadRight => "D-Right",
                _ => $"Button {(int)joyBtn.ButtonIndex}"
            };
        }
        if (ev is InputEventJoypadMotion joyMotion)
        {
            string axisName = joyMotion.Axis switch
            {
                JoyAxis.LeftX => "L-Stick X",
                JoyAxis.LeftY => "L-Stick Y",
                JoyAxis.RightX => "R-Stick X",
                JoyAxis.RightY => "R-Stick Y",
                JoyAxis.TriggerLeft => "LT",
                JoyAxis.TriggerRight => "RT",
                _ => $"Axis {(int)joyMotion.Axis}"
            };
            string direction = joyMotion.AxisValue > 0 ? "+" : "-";
            return $"{axisName} {direction}";
        }
        return ev.AsText();
    }

    private void RemoveExistingBinding(string action, bool isGamepad)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
        {
            bool isJoypadEvent = ev is InputEventJoypadButton;
            if (isJoypadEvent == isGamepad)
            {
                InputMap.ActionEraseEvent(action, ev);
                break;
            }
        }
    }

    private void SyncUiActions(string action)
    {
        var uiAction = action switch
        {
            "cursor_up" => "ui_up",
            "cursor_down" => "ui_down",
            "cursor_left" => "ui_left",
            "cursor_right" => "ui_right",
            "interact_main" => "ui_accept",
            _ => null
        };

        if (uiAction == null) return;

        var events = InputMap.ActionGetEvents(action);
        var uiEvents = InputMap.ActionGetEvents(uiAction);

        foreach (var ev in events)
        {
            bool exists = false;
            foreach (var uiEv in uiEvents)
            {
                if (ev.IsMatch(uiEv, true))
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                InputMap.ActionAddEvent(uiAction, ev);
            }
        }
    }

    private void SetupFocusNeighbors()
    {
        // 不设置 FocusNeighbor，完全由手动导航控制
    }
}
