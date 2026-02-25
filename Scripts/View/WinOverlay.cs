using System;
using System.Collections.Generic;
using Godot;
using PictoMino.View.Effects;

namespace PictoMino.View;

/// <summary>
/// 胜利结算界面。显示关卡完成信息和选项。
/// </summary>
public partial class WinOverlay : CanvasLayer
{
    private Panel? _panel;
    private Label? _titleLabel;
    private Label? _timeLabel;
    private Label? _bestTimeLabel;
    private Button? _nextLevelButton;
    private Button? _retryButton;
    private Button? _menuButton;
    private List<Button> _buttons = new();
    private int _focusedIndex = 0;

    /// <summary>标题文本</summary>
    [Export] public string TitleText { get; set; } = "恭喜过关！";

    /// <summary>
    /// 当点击下一关按钮时触发。
    /// </summary>
    public event Action? OnNextLevel;

    /// <summary>
    /// 当点击重试按钮时触发。
    /// </summary>
    public event Action? OnRetry;

    /// <summary>
    /// 当点击返回菜单按钮时触发。
    /// </summary>
    public event Action? OnBackToMenu;

    public override void _Ready()
    {
        CreateUI();
        Hide();
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        if (Godot.Input.IsActionJustPressed("cursor_left") || Godot.Input.IsActionJustPressed("ui_left"))
        {
            GameSession.Instance.LastInputWasGamepad = true;
            NavigateFocus(-1);
        }
        else if (Godot.Input.IsActionJustPressed("cursor_right") || Godot.Input.IsActionJustPressed("ui_right"))
        {
            GameSession.Instance.LastInputWasGamepad = true;
            NavigateFocus(1);
        }
        else if (Godot.Input.IsActionJustPressed("interact_main") || Godot.Input.IsActionJustPressed("ui_accept"))
        {
            ActivateFocusedButton();
        }
    }

    private void NavigateFocus(int direction)
    {
        var visibleButtons = _buttons.FindAll(b => b.Visible);
        if (visibleButtons.Count == 0) return;

        int currentIdx = visibleButtons.IndexOf(_buttons[_focusedIndex]);
        if (currentIdx < 0) currentIdx = 0;

        currentIdx = (currentIdx + direction + visibleButtons.Count) % visibleButtons.Count;
        _focusedIndex = _buttons.IndexOf(visibleButtons[currentIdx]);
        UpdateFocusVisual();
    }

    private void ActivateFocusedButton()
    {
        if (_focusedIndex >= 0 && _focusedIndex < _buttons.Count && _buttons[_focusedIndex].Visible)
        {
            _buttons[_focusedIndex].EmitSignal("pressed");
        }
    }

    private void UpdateFocusVisual()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].Modulate = (i == _focusedIndex) ? new Color(1.2f, 1.2f, 0.8f) : Colors.White;
        }
    }

    /// <summary>
    /// 显示胜利界面。
    /// </summary>
    public void ShowWin(float time, float bestTime = 0, bool isNewRecord = false, bool hasNextLevel = true)
    {
        if (_timeLabel != null)
        {
            string timeText = "用时: " + FormatTime(time);
            if (isNewRecord)
            {
                timeText += " ★ 新纪录!";
            }
            _timeLabel.Text = timeText;
        }

        if (_bestTimeLabel != null)
        {
            _bestTimeLabel.Text = bestTime > 0 
                ? "最佳: " + FormatTime(bestTime) 
                : "";
        }

        if (_nextLevelButton != null)
        {
            _nextLevelButton.Visible = hasNextLevel;
        }

        Show();

        PlayWinParticles();

        _focusedIndex = hasNextLevel ? 0 : 1;
        UpdateFocusVisual();
    }

    private void PlayWinParticles()
    {
        var viewport = GetViewport();
        if (viewport == null) return;

        var size = viewport.GetVisibleRect().Size;
        var center = size / 2f;

        var effect = new WinParticleEffect();
        AddChild(effect);
        effect.Play(center);
    }

    /// <summary>
    /// 隐藏界面。
    /// </summary>
    public void HideOverlay()
    {
        Hide();
    }

    private void CreateUI()
    {
        var background = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f)
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);

        _panel = new Panel
        {
            CustomMinimumSize = new Vector2(400, 300)
        };
        
        var panelContainer = new CenterContainer();
        panelContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panelContainer.AddChild(_panel);
        AddChild(panelContainer);

        var vbox = new VBoxContainer
        {
            OffsetLeft = 20,
            OffsetTop = 20,
            OffsetRight = -20,
            OffsetBottom = -20
        };
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 20);
        _panel.AddChild(vbox);

        _titleLabel = new Label
        {
            Text = TitleText,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 32);
        vbox.AddChild(_titleLabel);

        _timeLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _timeLabel.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(_timeLabel);

        _bestTimeLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(_bestTimeLabel);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });

        var buttonContainer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttonContainer.AddThemeConstantOverride("separation", 20);
        vbox.AddChild(buttonContainer);

        _nextLevelButton = new Button
        {
            Text = "下一关",
            CustomMinimumSize = new Vector2(100, 40),
            FocusMode = Control.FocusModeEnum.None
        };
        _nextLevelButton.Pressed += () => OnNextLevel?.Invoke();
        buttonContainer.AddChild(_nextLevelButton);
        _buttons.Add(_nextLevelButton);

        _retryButton = new Button
        {
            Text = "重试",
            CustomMinimumSize = new Vector2(100, 40),
            FocusMode = Control.FocusModeEnum.None
        };
        _retryButton.Pressed += () => OnRetry?.Invoke();
        buttonContainer.AddChild(_retryButton);
        _buttons.Add(_retryButton);

        _menuButton = new Button
        {
            Text = "菜单",
            CustomMinimumSize = new Vector2(100, 40),
            FocusMode = Control.FocusModeEnum.None
        };
        _menuButton.Pressed += () => OnBackToMenu?.Invoke();
        buttonContainer.AddChild(_menuButton);
        _buttons.Add(_menuButton);
    }

    private static string FormatTime(float seconds)
    {
        int mins = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        int ms = (int)((seconds % 1) * 100);

        if (mins > 0)
        {
            return mins + ":" + secs.ToString("D2") + "." + ms.ToString("D2");
        }
        return secs + "." + ms.ToString("D2") + "s";
    }
}
