using Godot;
using PictoMino.Core;

namespace PictoMino.View;

/// <summary>
/// 游戏会话单例。用于在场景间传递关卡数据。
/// </summary>
public partial class GameSession : Node
{
    private static GameSession? _instance;

    /// <summary>单例实例</summary>
    public static GameSession Instance => _instance!;

    /// <summary>待加载的关卡数据</summary>
    public LevelData? PendingLevel { get; set; }

    /// <summary>关卡管理器</summary>
    public LevelManager LevelManager { get; } = new();

    /// <summary>是否已初始化关卡</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>上次使用的输入设备是否为键盘/手柄</summary>
    public bool LastInputWasGamepad { get; set; } = false;

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Ready()
    {
        InitializeLevelManager();
    }

    /// <summary>
    /// 初始化关卡管理器。
    /// </summary>
    private void InitializeLevelManager()
    {
        if (IsInitialized) return;

        var chapters = GodotLevelLoader.LoadAllChapters();
        if (chapters.Count > 0)
        {
            foreach (var chapter in chapters)
            {
                LevelManager.AddChapter(chapter);
            }
            GD.Print($"GameSession: Loaded {LevelManager.TotalLevelCount} levels.");
        }
        else
        {
            LevelManager.AddChapter(Core.LevelManager.CreateTutorialChapter());
            GD.Print($"GameSession: Loaded {LevelManager.TotalLevelCount} built-in levels.");
        }

        IsInitialized = true;
    }

    /// <summary>
    /// 选择关卡并切换到游戏场景。
    /// </summary>
    public void StartLevel(LevelData level)
    {
        PendingLevel = level;
        GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
    }

    /// <summary>
    /// 选择关卡（通过 ID）并切换到游戏场景。
    /// </summary>
    public void StartLevel(string levelId)
    {
        var level = LevelManager.GetLevel(levelId);
        if (level != null)
        {
            StartLevel(level);
        }
        else
        {
            GD.PrintErr($"GameSession: Level '{levelId}' not found.");
        }
    }

    /// <summary>
    /// 返回关卡选择场景。
    /// </summary>
    public void BackToLevelSelect()
    {
        PendingLevel = null;
        GetTree().ChangeSceneToFile("res://Scenes/LevelSelect.tscn");
    }

    /// <summary>
    /// 前往标题场景。
    /// </summary>
    public void GoToTitle()
    {
        PendingLevel = null;
        GetTree().ChangeSceneToFile("res://Scenes/Title.tscn");
    }

    /// <summary>
    /// 前往关卡选择场景。
    /// </summary>
    public void GoToLevelSelect()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelSelect.tscn");
    }

    /// <summary>
    /// 前往设置场景。
    /// </summary>
    public void GoToSettings()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Settings.tscn");
    }

    /// <summary>
    /// 前往关卡编辑器场景。
    /// </summary>
    public void GoToLevelEditor()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelEditor.tscn");
    }
}
