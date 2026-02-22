using Godot;

namespace PictoMino.Input;

/// <summary>
/// 输入策略接口。定义 Ghost Hand 系统的输入行为。
/// </summary>
public interface IInputStrategy
{
    /// <summary>
    /// 当策略被激活时触发的初始化。
    /// </summary>
    void OnActivate();

    /// <summary>
    /// 当策略被停用时触发的清理。
    /// </summary>
    void OnDeactivate();

    /// <summary>
    /// 处理输入事件。
    /// </summary>
    /// <param name="event">Godot 输入事件</param>
    /// <returns>true 表示此策略已处理该事件</returns>
    bool HandleInput(InputEvent @event);

    /// <summary>
    /// 每帧更新。用于持续性输入处理（如鼠标位置跟踪）。
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    void Process(double delta);

    /// <summary>
    /// 获取当前 Ghost 的目标位置（棋盘坐标）。
    /// </summary>
    Vector2I? GetGhostGridPosition();
}
