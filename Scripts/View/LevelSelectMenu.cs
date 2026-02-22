using System;
using System.Linq;
using Godot;
using PictoMino.Core;

namespace PictoMino.View;

/// <summary>
/// 关卡选择菜单。
/// </summary>
public partial class LevelSelectMenu : CanvasLayer
{
    private LevelManager? _levelManager;
    private VBoxContainer? _chapterContainer;
    private Label? _titleLabel;
    private Button? _backButton;

    /// <summary>关卡按钮尺寸</summary>
    [Export] public Vector2 LevelButtonSize { get; set; } = new Vector2(80, 80);

    /// <summary>
    /// 当选择关卡时触发。参数为关卡 ID。
    /// </summary>
    public event Action<string>? OnLevelSelected;

    /// <summary>
    /// 当点击返回时触发。
    /// </summary>
    public event Action? OnBack;

    /// <summary>
    /// 绑定的关卡管理器。
    /// </summary>
    public LevelManager? LevelManager
    {
        get => _levelManager;
        set
        {
            _levelManager = value;
            RefreshUI();
        }
    }

    public override void _Ready()
    {
        CreateUI();
        Hide();
    }

    /// <summary>
    /// 显示菜单。
    /// </summary>
    public void ShowMenu()
    {
        RefreshUI();
        Show();
        FocusFirstAvailable();
    }

    /// <summary>
    /// 隐藏菜单。
    /// </summary>
    public void HideMenu()
    {
        Hide();
    }

    private void CreateUI()
    {
        var background = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.15f, 1f)
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);

        var mainContainer = new MarginContainer();
        mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        mainContainer.AddThemeConstantOverride("margin_left", 40);
        mainContainer.AddThemeConstantOverride("margin_right", 40);
        mainContainer.AddThemeConstantOverride("margin_top", 40);
        mainContainer.AddThemeConstantOverride("margin_bottom", 40);
        AddChild(mainContainer);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 30);
        mainContainer.AddChild(vbox);

        var titleBar = new HBoxContainer();
        vbox.AddChild(titleBar);

        _backButton = new Button
        {
            Text = "← 返回",
            CustomMinimumSize = new Vector2(100, 40)
        };
        _backButton.Pressed += () => OnBack?.Invoke();
        titleBar.AddChild(_backButton);

        var spacer1 = new Control();
        spacer1.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        titleBar.AddChild(spacer1);

        _titleLabel = new Label
        {
            Text = "选择关卡"
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 36);
        titleBar.AddChild(_titleLabel);

        var spacer2 = new Control();
        spacer2.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        titleBar.AddChild(spacer2);

        var progressLabel = new Label
        {
            CustomMinimumSize = new Vector2(100, 40)
        };
        titleBar.AddChild(progressLabel);

        var scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(scrollContainer);

        _chapterContainer = new VBoxContainer();
        _chapterContainer.AddThemeConstantOverride("separation", 40);
        scrollContainer.AddChild(_chapterContainer);
    }

    private void RefreshUI()
    {
        if (_chapterContainer == null || _levelManager == null) return;

        foreach (var child in _chapterContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var chapter in _levelManager.Chapters)
        {
            CreateChapterUI(chapter);
        }
    }

    private void CreateChapterUI(LevelChapter chapter)
    {
        if (_chapterContainer == null || _levelManager == null) return;

        var chapterBox = new VBoxContainer();
        chapterBox.AddThemeConstantOverride("separation", 15);
        _chapterContainer.AddChild(chapterBox);

        var chapterLabel = new Label
        {
            Text = chapter.Name
        };
        chapterLabel.AddThemeFontSizeOverride("font_size", 24);
        chapterBox.AddChild(chapterLabel);

        var gridContainer = new HFlowContainer();
        gridContainer.AddThemeConstantOverride("h_separation", 15);
        gridContainer.AddThemeConstantOverride("v_separation", 15);
        chapterBox.AddChild(gridContainer);

        for (int i = 0; i < chapter.Levels.Length; i++)
        {
            var level = chapter.Levels[i];
            CreateLevelButton(gridContainer, level, i + 1);
        }
    }

    private void CreateLevelButton(HFlowContainer container, LevelData level, int displayNumber)
    {
        if (_levelManager == null) return;

        var progress = _levelManager.GetProgress(level.Id);
        bool isUnlocked = _levelManager.IsUnlocked(level.Id);

        var button = new Button
        {
            CustomMinimumSize = LevelButtonSize,
            Disabled = !isUnlocked
        };

        if (!isUnlocked)
        {
            button.Text = "🔒";
            button.TooltipText = "完成前一关以解锁";
        }
        else if (progress.IsCompleted)
        {
            button.Text = "✓\n" + displayNumber.ToString();
            button.TooltipText = level.Name + "\n最佳: " + FormatTime(progress.BestTime);
        }
        else
        {
            button.Text = displayNumber.ToString();
            button.TooltipText = level.Name;
        }

        var modulate = level.Difficulty switch
        {
            1 => new Color(0.6f, 0.9f, 0.6f),
            2 => new Color(0.9f, 0.9f, 0.5f),
            3 => new Color(0.9f, 0.7f, 0.4f),
            4 => new Color(0.9f, 0.5f, 0.4f),
            _ => new Color(0.9f, 0.4f, 0.9f)
        };

        if (isUnlocked)
        {
            button.Modulate = modulate;
        }

        string levelId = level.Id;
        button.Pressed += () => OnLevelSelected?.Invoke(levelId);
        container.AddChild(button);
    }

    private void FocusFirstAvailable()
    {
        if (_chapterContainer == null) return;

        foreach (var node in _chapterContainer.GetChildren())
        {
            if (node is VBoxContainer chapterBox)
            {
                foreach (var child in chapterBox.GetChildren())
                {
                    if (child is HFlowContainer grid)
                    {
                        foreach (var gridChild in grid.GetChildren())
                        {
                            if (gridChild is Button btn && !btn.Disabled)
                            {
                                btn.GrabFocus();
                                return;
                            }
                        }
                    }
                }
            }
        }

        _backButton?.GrabFocus();
    }

    private static string FormatTime(float seconds)
    {
        if (seconds <= 0) return "-";
        int secs = (int)seconds;
        int ms = (int)((seconds % 1) * 100);
        return secs + "." + ms.ToString("D2") + "s";
    }
}
