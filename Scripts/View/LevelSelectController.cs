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
            _levelSelectMenu.ShowMenu();
        }
    }

    private void OnLevelSelected(string levelId)
    {
        GameSession.Instance.StartLevel(levelId);
    }

    private void OnBack()
    {
        // 暂时无操作，后续可返回主菜单
    }
}
