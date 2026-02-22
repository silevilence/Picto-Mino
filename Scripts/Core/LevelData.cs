namespace PictoMino.Core;

/// <summary>
/// 关卡数据。封装一个完整关卡所需的所有信息。
/// </summary>
public record LevelData
{
    /// <summary>关卡唯一标识</summary>
    public required string Id { get; init; }

    /// <summary>关卡显示名称</summary>
    public required string Name { get; init; }

    /// <summary>关卡难度 (1-5)</summary>
    public int Difficulty { get; init; } = 1;

    /// <summary>棋盘行数</summary>
    public required int Rows { get; init; }

    /// <summary>棋盘列数</summary>
    public required int Cols { get; init; }

    /// <summary>
    /// 目标图案。true 表示应填充，false 表示应为空。
    /// 为 null 时表示需要全部填满。
    /// </summary>
    public bool[,]? Target { get; init; }

    /// <summary>
    /// 可用形状列表。
    /// </summary>
    public required ShapeData[] Shapes { get; init; }

    /// <summary>
    /// 是否已解锁。
    /// </summary>
    public bool IsUnlocked { get; init; } = true;

    /// <summary>
    /// 是否已完成。
    /// </summary>
    public bool IsCompleted { get; init; } = false;

    /// <summary>
    /// 最佳完成时间（秒）。0 表示未完成。
    /// </summary>
    public float BestTime { get; init; } = 0;
}

/// <summary>
/// 关卡集合（章节）。
/// </summary>
public record LevelChapter
{
    /// <summary>章节唯一标识</summary>
    public required string Id { get; init; }

    /// <summary>章节名称</summary>
    public required string Name { get; init; }

    /// <summary>章节内的关卡列表</summary>
    public required LevelData[] Levels { get; init; }
}
