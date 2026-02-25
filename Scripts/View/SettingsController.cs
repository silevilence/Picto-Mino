using Godot;

namespace PictoMino.View;

/// <summary>
/// 设置场景控制器。
/// </summary>
public partial class SettingsController : Control
{
    private SettingsMenu? _settingsMenu;
    
    private float _repeatDelay = 0.4f;
    private float _repeatInterval = 0.08f;
    private float _repeatTimer = 0f;
    private int _repeatDirection = 0;

    public override void _Ready()
    {
        _settingsMenu = GetNode<SettingsMenu>("SettingsMenu");
        _settingsMenu.OnBack += OnBack;
        _settingsMenu.ShowMenu();
    }

    public override void _Process(double delta)
    {
        if (_settingsMenu == null || !_settingsMenu.Visible) return;
        if (_settingsMenu.IsWaitingForInput) return;
        
        if (_settingsMenu.JustFinishedBinding)
        {
            _settingsMenu.ClearJustFinishedBinding();
            _repeatDirection = 0;
            return;
        }

        bool downPressed = Godot.Input.IsActionPressed("cursor_down") || Godot.Input.IsActionPressed("ui_down");
        bool upPressed = Godot.Input.IsActionPressed("cursor_up") || Godot.Input.IsActionPressed("ui_up");

        if (Godot.Input.IsActionJustPressed("cursor_down") || Godot.Input.IsActionJustPressed("ui_down"))
        {
            _settingsMenu.NavigateFocus(1);
            _repeatDirection = 1;
            _repeatTimer = _repeatDelay;
        }
        else if (Godot.Input.IsActionJustPressed("cursor_up") || Godot.Input.IsActionJustPressed("ui_up"))
        {
            _settingsMenu.NavigateFocus(-1);
            _repeatDirection = -1;
            _repeatTimer = _repeatDelay;
        }
        else if (downPressed && _repeatDirection == 1)
        {
            _repeatTimer -= (float)delta;
            if (_repeatTimer <= 0)
            {
                _settingsMenu.NavigateFocus(1);
                _repeatTimer = _repeatInterval;
            }
        }
        else if (upPressed && _repeatDirection == -1)
        {
            _repeatTimer -= (float)delta;
            if (_repeatTimer <= 0)
            {
                _settingsMenu.NavigateFocus(-1);
                _repeatTimer = _repeatInterval;
            }
        }
        else if (!downPressed && !upPressed)
        {
            _repeatDirection = 0;
        }

        if (Godot.Input.IsActionJustPressed("interact_main") || Godot.Input.IsActionJustPressed("ui_accept"))
        {
            _settingsMenu.ActivateFocusedButton();
        }
        else if (Godot.Input.IsActionJustPressed("interact_secondary") || Godot.Input.IsActionJustPressed("ui_cancel"))
        {
            _settingsMenu.FocusBackButton();
        }
    }

    private void OnBack()
    {
        GameSession.Instance.GoToTitle();
    }
}
