using System;
using System.Collections.Generic;
using Godot;

namespace PictoMino.View;

/// <summary>
/// 暂停菜单。
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    private Control? _root;
    private VBoxContainer? _mainMenu;
    private VBoxContainer? _confirmMenu;
    private List<Button> _menuButtons = new();
    private List<Button> _confirmButtons = new();
    private int _focusedIndex = 0;
    private bool _isConfirming = false;

    public event Action? OnResume;
    public event Action? OnBackToLevelSelect;
    public event Action? OnBackToTitle;
    public event Action? OnExit;

    public override void _Ready()
    {
        Layer = 100;
        CreateUI();
        Hide();
    }

    private bool _justOpened = false;

    public override void _Process(double delta)
    {
        if (!Visible) return;

        if (_root != null)
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            if (_root.Size != viewportSize)
            {
                _root.Size = viewportSize;
            }
        }

        if (_justOpened)
        {
            _justOpened = false;
            return;
        }

        if (Godot.Input.IsActionJustPressed("cursor_down") || Godot.Input.IsActionJustPressed("ui_down"))
        {
            NavigateFocus(1);
        }
        else if (Godot.Input.IsActionJustPressed("cursor_up") || Godot.Input.IsActionJustPressed("ui_up"))
        {
            NavigateFocus(-1);
        }
        else if (Godot.Input.IsActionJustPressed("interact_main") || Godot.Input.IsActionJustPressed("ui_accept"))
        {
            ActivateFocusedButton();
        }
        else if (Godot.Input.IsActionJustPressed("pause_game") || Godot.Input.IsActionJustPressed("interact_secondary"))
        {
            if (_isConfirming)
            {
                HideConfirmMenu();
            }
            else
            {
                OnResume?.Invoke();
            }
        }
    }

    public void ShowMenu()
    {
        if (_mainMenu == null || _confirmMenu == null)
        {
            GD.PrintErr("PauseMenu: UI not created yet!");
            return;
        }
        _isConfirming = false;
        _mainMenu.Visible = true;
        _confirmMenu.Visible = false;
        _focusedIndex = 0;
        _justOpened = true;
        UpdateFocusVisual();
        Show();
    }

    public void HideMenu()
    {
        Hide();
    }

    private void NavigateFocus(int direction)
    {
        var buttons = _isConfirming ? _confirmButtons : _menuButtons;
        if (buttons.Count == 0) return;

        _focusedIndex = (_focusedIndex + direction + buttons.Count) % buttons.Count;
        UpdateFocusVisual();
    }

    private void ActivateFocusedButton()
    {
        var buttons = _isConfirming ? _confirmButtons : _menuButtons;
        if (_focusedIndex >= 0 && _focusedIndex < buttons.Count)
        {
            buttons[_focusedIndex].EmitSignal("pressed");
        }
    }

    private void UpdateFocusVisual()
    {
        var buttons = _isConfirming ? _confirmButtons : _menuButtons;
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].Modulate = (i == _focusedIndex) ? new Color(1.2f, 1.2f, 0.8f) : Colors.White;
        }
    }

    private void CreateUI()
    {
        _root = new Control();
        AddChild(_root);
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.SetDeferred("size", GetViewport().GetVisibleRect().Size);

        var background = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.7f)
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(background);

        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(centerContainer);

        var panel = new PanelContainer();
        centerContainer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 40);
        margin.AddThemeConstantOverride("margin_right", 40);
        margin.AddThemeConstantOverride("margin_top", 30);
        margin.AddThemeConstantOverride("margin_bottom", 30);
        panel.AddChild(margin);

        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 20);
        margin.AddChild(container);

        var titleLabel = new Label
        {
            Text = "暂停",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        container.AddChild(titleLabel);

        _mainMenu = new VBoxContainer();
        _mainMenu.AddThemeConstantOverride("separation", 10);
        container.AddChild(_mainMenu);

        CreateMenuButton(_mainMenu, _menuButtons, "继续游戏", () => OnResume?.Invoke());
        CreateMenuButton(_mainMenu, _menuButtons, "返回关卡选择", () => OnBackToLevelSelect?.Invoke());
        CreateMenuButton(_mainMenu, _menuButtons, "返回标题", () => OnBackToTitle?.Invoke());
        CreateMenuButton(_mainMenu, _menuButtons, "退出游戏", ShowConfirmMenu);

        _confirmMenu = new VBoxContainer();
        _confirmMenu.AddThemeConstantOverride("separation", 10);
        _confirmMenu.Visible = false;
        container.AddChild(_confirmMenu);

        var confirmLabel = new Label
        {
            Text = "确定要退出游戏吗？",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _confirmMenu.AddChild(confirmLabel);

        var confirmButtonsContainer = new HBoxContainer();
        confirmButtonsContainer.AddThemeConstantOverride("separation", 20);
        confirmButtonsContainer.Alignment = BoxContainer.AlignmentMode.Center;
        _confirmMenu.AddChild(confirmButtonsContainer);

        CreateConfirmButton(confirmButtonsContainer, "取消", HideConfirmMenu);
        CreateConfirmButton(confirmButtonsContainer, "确定", () => OnExit?.Invoke());
    }

    private void CreateMenuButton(VBoxContainer container, List<Button> list, string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(200, 40),
            FocusMode = Control.FocusModeEnum.None
        };
        button.Pressed += onPressed;
        container.AddChild(button);
        list.Add(button);
    }

    private void CreateConfirmButton(HBoxContainer container, string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(100, 40),
            FocusMode = Control.FocusModeEnum.None
        };
        button.Pressed += onPressed;
        container.AddChild(button);
        _confirmButtons.Add(button);
    }

    private void ShowConfirmMenu()
    {
        _isConfirming = true;
        _mainMenu!.Visible = false;
        _confirmMenu!.Visible = true;
        _focusedIndex = 0;
        UpdateFocusVisual();
    }

    private void HideConfirmMenu()
    {
        _isConfirming = false;
        _mainMenu!.Visible = true;
        _confirmMenu!.Visible = false;
        _focusedIndex = 0;
        UpdateFocusVisual();
    }
}
