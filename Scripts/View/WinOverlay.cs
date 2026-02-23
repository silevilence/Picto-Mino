using System;
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

        if (hasNextLevel)
        {
            _nextLevelButton?.GrabFocus();
        }
        else
        {
            _retryButton?.GrabFocus();
        }
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
            CustomMinimumSize = new Vector2(100, 40)
        };
        _nextLevelButton.Pressed += () => OnNextLevel?.Invoke();
        buttonContainer.AddChild(_nextLevelButton);

        _retryButton = new Button
        {
            Text = "重试",
            CustomMinimumSize = new Vector2(100, 40)
        };
        _retryButton.Pressed += () => OnRetry?.Invoke();
        buttonContainer.AddChild(_retryButton);

        _menuButton = new Button
        {
            Text = "菜单",
            CustomMinimumSize = new Vector2(100, 40)
        };
        _menuButton.Pressed += () => OnBackToMenu?.Invoke();
        buttonContainer.AddChild(_menuButton);
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
