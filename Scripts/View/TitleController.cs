using Godot;

namespace PictoMino.View;

/// <summary>
/// 标题场景控制器。
/// </summary>
public partial class TitleController : Control
{
    private Button? _selectLevelButton;
    private Button? _levelEditorButton;
    private Button? _settingsButton;
    private Button? _exitButton;

    public override void _Ready()
    {
        _selectLevelButton = GetNode<Button>("CenterContainer/MainVBox/ButtonContainer/SelectLevelButton");
        _levelEditorButton = GetNode<Button>("CenterContainer/MainVBox/ButtonContainer/LevelEditorButton");
        _settingsButton = GetNode<Button>("CenterContainer/MainVBox/ButtonContainer/SettingsButton");
        _exitButton = GetNode<Button>("CenterContainer/MainVBox/ButtonContainer/ExitButton");

        _selectLevelButton.Pressed += OnSelectLevel;
        _levelEditorButton.Pressed += OnLevelEditor;
        _settingsButton.Pressed += OnSettings;
        _exitButton.Pressed += OnExit;

        _selectLevelButton.GrabFocus();
    }

    private void OnSelectLevel()
    {
        GameSession.Instance.GoToLevelSelect();
    }

    private void OnLevelEditor()
    {
        GameSession.Instance.GoToLevelEditor();
    }

    private void OnSettings()
    {
        GameSession.Instance.GoToSettings();
    }

    private void OnExit()
    {
        GetTree().Quit();
    }
}
