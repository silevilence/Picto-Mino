using Godot;
using PictoMino.Core;

namespace PictoMino.View;

/// <summary>
/// 关卡选择场景控制器。
/// </summary>
public partial class LevelSelectController : Control
{
    private LevelSelectMenu? _levelSelectMenu;

    public override void _Ready()
    {
        _levelSelectMenu = GetNodeOrNull<LevelSelectMenu>("%LevelSelectMenu");

        if (_levelSelectMenu != null)
        {
            _levelSelectMenu.LevelManager = GameSession.Instance.LevelManager;
            _levelSelectMenu.OnLevelSelected += OnLevelSelected;
            _levelSelectMenu.OnBack += OnBack;
            _levelSelectMenu.OnImportLevel += OnImportLevel;
            _levelSelectMenu.ShowMenu();
        }
    }

    public override void _Process(double delta)
    {
        if (_levelSelectMenu == null || !_levelSelectMenu.Visible) return;

        if (Godot.Input.IsActionJustPressed("cursor_right") || Godot.Input.IsActionJustPressed("ui_right"))
        {
            GameSession.Instance.LastInputWasGamepad = true;
            _levelSelectMenu.NavigateFocus(1);
        }
        else if (Godot.Input.IsActionJustPressed("cursor_left") || Godot.Input.IsActionJustPressed("ui_left"))
        {
            GameSession.Instance.LastInputWasGamepad = true;
            _levelSelectMenu.NavigateFocus(-1);
        }
        else if (Godot.Input.IsActionJustPressed("cursor_down") || Godot.Input.IsActionJustPressed("ui_down"))
        {
            GameSession.Instance.LastInputWasGamepad = true;
            _levelSelectMenu.NavigateFocus(1);
        }
        else if (Godot.Input.IsActionJustPressed("cursor_up") || Godot.Input.IsActionJustPressed("ui_up"))
        {
            GameSession.Instance.LastInputWasGamepad = true;
            _levelSelectMenu.NavigateFocus(-1);
        }
        else if (Godot.Input.IsActionJustPressed("interact_main") || Godot.Input.IsActionJustPressed("ui_accept"))
        {
            _levelSelectMenu.ActivateFocusedButton();
        }
        else if (Godot.Input.IsActionJustPressed("interact_secondary") || Godot.Input.IsActionJustPressed("ui_cancel"))
        {
            _levelSelectMenu.FocusBackButton();
        }
    }

    private void OnLevelSelected(string levelId)
    {
        GameSession.Instance.StartLevel(levelId);
    }

    private void OnBack()
    {
        GameSession.Instance.GoToTitle();
    }

    private void OnImportLevel(LevelData level)
    {
        GameSession.Instance.StartLevel(level);
    }
}
