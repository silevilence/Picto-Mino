namespace PictoMino.Core;

/// <summary>
/// 关卡管理器。管理所有关卡数据和进度。
/// </summary>
public class LevelManager
{
    private readonly List<LevelChapter> _chapters = new();
    private readonly Dictionary<string, LevelData> _levelsById = new();
    private readonly Dictionary<string, LevelProgress> _progress = new();

    /// <summary>所有章节（只读）</summary>
    public IReadOnlyList<LevelChapter> Chapters => _chapters;

    /// <summary>关卡总数</summary>
    public int TotalLevelCount => _levelsById.Count;

    /// <summary>已完成关卡数</summary>
    public int CompletedLevelCount => _progress.Count(p => p.Value.IsCompleted);

    /// <summary>
    /// 当关卡进度更新时触发。
    /// </summary>
    public event Action<string, LevelProgress>? OnProgressUpdated;

    /// <summary>
    /// 添加章节。
    /// </summary>
    public void AddChapter(LevelChapter chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);
        _chapters.Add(chapter);
        
        foreach (var level in chapter.Levels)
        {
            _levelsById[level.Id] = level;
        }
    }

    /// <summary>
    /// 根据 ID 获取关卡。
    /// </summary>
    public LevelData? GetLevel(string id)
    {
        return _levelsById.TryGetValue(id, out var level) ? level : null;
    }

    /// <summary>
    /// 获取关卡进度。
    /// </summary>
    public LevelProgress GetProgress(string levelId)
    {
        return _progress.TryGetValue(levelId, out var progress)
            ? progress
            : new LevelProgress { LevelId = levelId };
    }

    /// <summary>
    /// 更新关卡进度。
    /// </summary>
    public void UpdateProgress(string levelId, bool completed, float time = 0)
    {
        var existing = GetProgress(levelId);
        
        var newProgress = new LevelProgress
        {
            LevelId = levelId,
            IsCompleted = completed || existing.IsCompleted,
            BestTime = existing.BestTime == 0 
                ? time 
                : (time > 0 ? Math.Min(existing.BestTime, time) : existing.BestTime),
            PlayCount = existing.PlayCount + 1
        };
        
        _progress[levelId] = newProgress;
        OnProgressUpdated?.Invoke(levelId, newProgress);
        
        // 解锁下一关
        if (completed)
        {
            UnlockNextLevel(levelId);
        }
    }

    /// <summary>
    /// 检查关卡是否已解锁。
    /// </summary>
    public bool IsUnlocked(string levelId)
    {
        // 找到关卡在章节中的位置
        foreach (var chapter in _chapters)
        {
            for (int i = 0; i < chapter.Levels.Length; i++)
            {
                if (chapter.Levels[i].Id == levelId)
                {
                    // 第一关始终解锁
                    if (i == 0) return true;
                    
                    // 前一关完成才解锁
                    var prevLevelId = chapter.Levels[i - 1].Id;
                    return GetProgress(prevLevelId).IsCompleted;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// 获取下一个关卡 ID。
    /// </summary>
    public string? GetNextLevelId(string currentLevelId)
    {
        foreach (var chapter in _chapters)
        {
            for (int i = 0; i < chapter.Levels.Length - 1; i++)
            {
                if (chapter.Levels[i].Id == currentLevelId)
                {
                    return chapter.Levels[i + 1].Id;
                }
            }
        }
        return null;
    }

    private void UnlockNextLevel(string completedLevelId)
    {
        var nextId = GetNextLevelId(completedLevelId);
        // 解锁逻辑由 IsUnlocked 动态计算，无需额外处理
    }

    /// <summary>
    /// 重置所有进度。
    /// </summary>
    public void ResetAllProgress()
    {
        _progress.Clear();
    }

    /// <summary>
    /// 生成内置教程关卡。
    /// </summary>
    public static LevelChapter CreateTutorialChapter()
    {
        var levels = new List<LevelData>();

        // 关卡 1: 简单 2x2
        levels.Add(new LevelData
        {
            Id = "tutorial_01",
            Name = "初识拼图",
            Difficulty = 1,
            Rows = 2,
            Cols = 2,
            Shapes = new[]
            {
                new ShapeData(new bool[,] { { true, true }, { true, true } }) // 2x2 方块
            }
        });

        // 关卡 2: 3x2 两块
        levels.Add(new LevelData
        {
            Id = "tutorial_02",
            Name = "初试身手",
            Difficulty = 1,
            Rows = 2,
            Cols = 3,
            Shapes = new[]
            {
                new ShapeData(new bool[,] { { true, true, true } }),
                new ShapeData(new bool[,] { { true, true, true } })
            }
        });

        // 关卡 3: 3x3 L形
        levels.Add(new LevelData
        {
            Id = "tutorial_03",
            Name = "L形初探",
            Difficulty = 1,
            Rows = 3,
            Cols = 3,
            Shapes = new[]
            {
                new ShapeData(new bool[,] { { true, true }, { true, false }, { true, false } }), // L
                new ShapeData(new bool[,] { { true }, { true }, { true } }), // I
                new ShapeData(new bool[,] { { true, true } }) // 横条
            }
        });

        // 关卡 4: 4x4 多形状
        levels.Add(new LevelData
        {
            Id = "tutorial_04",
            Name = "方块组合",
            Difficulty = 2,
            Rows = 4,
            Cols = 4,
            Shapes = new[]
            {
                new ShapeData(new bool[,] { { true, true }, { true, true } }), // O
                new ShapeData(new bool[,] { { true, true }, { true, true } }), // O
                new ShapeData(new bool[,] { { true, true }, { true, true } }), // O
                new ShapeData(new bool[,] { { true, true }, { true, true } })  // O
            }
        });

        // 关卡 5: 5x4 T形
        levels.Add(new LevelData
        {
            Id = "tutorial_05",
            Name = "T形挑战",
            Difficulty = 2,
            Rows = 4,
            Cols = 5,
            Shapes = new[]
            {
                new ShapeData(new bool[,] { { true, true, true }, { false, true, false } }), // T (4格)
                new ShapeData(new bool[,] { { true, true, true }, { false, true, false } }), // T (4格)
                new ShapeData(new bool[,] { { true, true }, { true, true } }), // O (4格)
                new ShapeData(new bool[,] { { true, true }, { true, true } }), // O (4格)
                new ShapeData(new bool[,] { { true, true, true, true } }) // I横条 (4格) - 总计 20格
            }
        });

        return new LevelChapter
        {
            Id = "tutorial",
            Name = "教程",
            Levels = levels.ToArray()
        };
    }
}

/// <summary>
/// 关卡进度数据。
/// </summary>
public record LevelProgress
{
    /// <summary>关卡 ID</summary>
    public required string LevelId { get; init; }

    /// <summary>是否已完成</summary>
    public bool IsCompleted { get; init; }

    /// <summary>最佳时间（秒）</summary>
    public float BestTime { get; init; }

    /// <summary>游玩次数</summary>
    public int PlayCount { get; init; }
}
