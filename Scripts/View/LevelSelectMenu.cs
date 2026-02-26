using System;
using System.Collections.Generic;
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
    private Button? _importButton;
    private FileDialog? _fileDialog;

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
    /// 当导入外部关卡时触发。参数为关卡数据。
    /// </summary>
    public event Action<LevelData>? OnImportLevel;

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

    private Control? _root;

    public override void _Ready()
    {
        CreateUI();
        Hide();
    }

    private Button? _focusedButton;
    private List<Button> _allButtons = new();

    public void NavigateFocus(int direction)
    {
        if (_allButtons.Count == 0) return;

        int currentIndex = _focusedButton != null ? _allButtons.IndexOf(_focusedButton) : -1;
        int newIndex = currentIndex;

        for (int i = 0; i < _allButtons.Count; i++)
        {
            newIndex = (newIndex + direction + _allButtons.Count) % _allButtons.Count;
            var btn = _allButtons[newIndex];
            if (GodotObject.IsInstanceValid(btn) && !btn.Disabled)
            {
                SetFocusedButton(btn);
                return;
            }
        }
    }

    private void SetFocusedButton(Button? button)
    {
        if (_focusedButton != null && GodotObject.IsInstanceValid(_focusedButton))
        {
            _focusedButton.ReleaseFocus();
        }
        _focusedButton = button;
        _focusedButton?.GrabFocus();
    }

    public void ActivateFocusedButton()
    {
        if (_focusedButton != null && GodotObject.IsInstanceValid(_focusedButton) && !_focusedButton.Disabled)
        {
            _focusedButton.EmitSignal("pressed");
        }
    }

    public void FocusBackButton()
    {
        if (_backButton != null && _allButtons.Contains(_backButton))
        {
            SetFocusedButton(_backButton);
        }
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

    /// <summary>
    /// 显示菜单。
    /// </summary>
    public void ShowMenu()
    {
        RefreshUI();
        Show();
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
        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _root.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
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
        vbox.AddThemeConstantOverride("separation", 30);
        mainContainer.AddChild(vbox);

        var titleBar = new HBoxContainer();
        vbox.AddChild(titleBar);

        _backButton = new Button
        {
            Text = "← 返回",
            CustomMinimumSize = new Vector2(100, 40),
            FocusMode = Control.FocusModeEnum.All
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

        _importButton = new Button
        {
            Text = "📂 导入关卡",
            CustomMinimumSize = new Vector2(120, 40),
            FocusMode = Control.FocusModeEnum.All
        };
        _importButton.Pressed += OnImportButtonPressed;
        titleBar.AddChild(_importButton);

        CreateFileDialog();

        var scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scrollContainer.FollowFocus = true;
        vbox.AddChild(scrollContainer);

        _chapterContainer = new VBoxContainer();
        _chapterContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _chapterContainer.AddThemeConstantOverride("separation", 40);
        scrollContainer.AddChild(_chapterContainer);
    }

    private void CreateFileDialog()
    {
        _fileDialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Title = "选择关卡文件",
            Size = new Vector2I(800, 600),
            Transient = false
        };
        _fileDialog.AddFilter("*.level", "关卡文件");
        _fileDialog.FileSelected += OnFileSelected;
        AddChild(_fileDialog);
    }

    private void OnImportButtonPressed()
    {
        _fileDialog?.PopupCentered();
    }

    private void OnFileSelected(string path)
    {
        _fileDialog?.Hide();
        
        var level = GodotLevelLoader.LoadLevelFromExternalFile(path);
        if (level != null)
        {
            OnImportLevel?.Invoke(level);
        }
        else
        {
            ShowImportError(path);
        }
    }

    private void ShowImportError(string path)
    {
        var dialog = new AcceptDialog
        {
            Title = "导入失败",
            DialogText = $"无法加载关卡文件:\n{path}\n\n请确保文件格式正确。",
            Transient = false
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void RefreshUI()
    {
        if (_chapterContainer == null || _levelManager == null) return;

        _allButtons.Clear();
        _focusedButton = null;

        foreach (var child in _chapterContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var chapter in _levelManager.Chapters)
        {
            CreateChapterUI(chapter);
        }

        CallDeferred(nameof(SetupFocusNeighbors));
    }

    private void SetupFocusNeighbors()
    {
        if (_chapterContainer == null) return;

        _allButtons.Clear();
        
        if (_backButton != null)
        {
            _allButtons.Add(_backButton);
        }

        if (_importButton != null)
        {
            _allButtons.Add(_importButton);
        }
        
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
                            if (gridChild is Button btn)
                            {
                                _allButtons.Add(btn);
                            }
                        }
                    }
                }
            }
        }

        for (int i = 0; i < _allButtons.Count; i++)
        {
            var btn = _allButtons[i];
            if (i > 0)
            {
                btn.FocusNeighborLeft = _allButtons[i - 1].GetPath();
                btn.FocusNeighborTop = _allButtons[i - 1].GetPath();
            }
            if (i < _allButtons.Count - 1)
            {
                btn.FocusNeighborRight = _allButtons[i + 1].GetPath();
                btn.FocusNeighborBottom = _allButtons[i + 1].GetPath();
            }
        }

        // 自动聚焦第一个可用的关卡按钮（跳过返回按钮）
        for (int i = 1; i < _allButtons.Count; i++)
        {
            if (!_allButtons[i].Disabled)
            {
                SetFocusedButton(_allButtons[i]);
                break;
            }
        }
    }

    private void CreateChapterUI(LevelChapter chapter)
    {
        if (_chapterContainer == null || _levelManager == null) return;

        var chapterBox = new VBoxContainer();
        chapterBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        chapterBox.AddThemeConstantOverride("separation", 15);
        _chapterContainer.AddChild(chapterBox);

        var chapterLabel = new Label
        {
            Text = chapter.Name
        };
        chapterLabel.AddThemeFontSizeOverride("font_size", 24);
        chapterBox.AddChild(chapterLabel);

        var gridContainer = new HFlowContainer();
        gridContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        gridContainer.AddThemeConstantOverride("h_separation", 15);
        gridContainer.AddThemeConstantOverride("v_separation", 15);
        gridContainer.ClipContents = false;
        chapterBox.AddChild(gridContainer);

        var spacer = new Control { CustomMinimumSize = new Vector2(4, 0) };
        gridContainer.AddChild(spacer);

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
            Disabled = !isUnlocked,
            FocusMode = Control.FocusModeEnum.All
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
