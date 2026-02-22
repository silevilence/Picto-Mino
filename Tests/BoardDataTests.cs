namespace PictoMino.Core.Tests;

[TestFixture]
public class BoardDataTests
{
    // ─── 基础功能 ───────────────────────────────────────

    [Test]
    public void NewBoard_AllCellsAreZero()
    {
        var board = new BoardData(5, 5);

        for (int r = 0; r < board.Rows; r++)
        for (int c = 0; c < board.Cols; c++)
            Assert.That(board.GetCell(r, c), Is.EqualTo(0));
    }

    [Test]
    public void SetCell_UpdatesValue()
    {
        var board = new BoardData(3, 3);
        board.SetCell(1, 2, 42);
        Assert.That(board.GetCell(1, 2), Is.EqualTo(42));
    }

    [Test]
    public void SetCell_RaisesOnCellChanged()
    {
        var board = new BoardData(3, 3);
        int eventRow = -1, eventCol = -1, eventVal = -1;
        board.OnCellChanged += (r, c, v) => { eventRow = r; eventCol = c; eventVal = v; };

        board.SetCell(0, 1, 7);

        Assert.That(eventRow, Is.EqualTo(0));
        Assert.That(eventCol, Is.EqualTo(1));
        Assert.That(eventVal, Is.EqualTo(7));
    }

    [Test]
    public void SetCell_SameValue_DoesNotRaiseEvent()
    {
        var board = new BoardData(3, 3);
        bool eventFired = false;
        board.OnCellChanged += (_, _, _) => eventFired = true;

        board.SetCell(0, 0, 0); // 默认已是 0
        Assert.That(eventFired, Is.False);
    }

    [Test]
    public void IsInBounds_ValidCoords_ReturnsTrue()
    {
        var board = new BoardData(5, 5);
        Assert.That(board.IsInBounds(0, 0), Is.True);
        Assert.That(board.IsInBounds(4, 4), Is.True);
    }

    [Test]
    public void IsInBounds_InvalidCoords_ReturnsFalse()
    {
        var board = new BoardData(5, 5);
        Assert.That(board.IsInBounds(-1, 0), Is.False);
        Assert.That(board.IsInBounds(0, 5), Is.False);
        Assert.That(board.IsInBounds(5, 0), Is.False);
    }

    [Test]
    public void GetCell_OutOfBounds_Throws()
    {
        var board = new BoardData(3, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => board.GetCell(3, 0));
    }

    [Test]
    public void Constructor_InvalidSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoardData(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoardData(5, -1));
    }

    // ─── Target 构造 ────────────────────────────────────

    [Test]
    public void Constructor_WithTarget_StoresTarget()
    {
        var target = new bool[,] { { true, false }, { false, true } };
        var board = new BoardData(2, 2, target);

        Assert.That(board.Target, Is.Not.Null);
        Assert.That(board.Target![0, 0], Is.True);
        Assert.That(board.Target[0, 1], Is.False);
    }

    [Test]
    public void Constructor_WithTarget_ClonesTarget()
    {
        var target = new bool[,] { { true, false }, { false, true } };
        var board = new BoardData(2, 2, target);

        target[0, 0] = false; // 修改原始数组
        Assert.That(board.Target![0, 0], Is.True); // 不受影响
    }

    [Test]
    public void Constructor_TargetDimensionMismatch_Throws()
    {
        var target = new bool[,] { { true, false, true } }; // 1x3，棋盘 2x2
        Assert.Throws<ArgumentException>(() => new BoardData(2, 2, target));
    }

    [Test]
    public void Constructor_NullTarget_TargetIsNull()
    {
        var board = new BoardData(3, 3);
        Assert.That(board.Target, Is.Null);
    }

    // ─── TryPlace ───────────────────────────────────────

    [Test]
    public void TryPlace_OnEmptyBoard_Succeeds()
    {
        var board = new BoardData(5, 5);
        // I 形横条：X X X
        var shape = new ShapeData(new bool[,] { { true, true, true } });

        bool result = board.TryPlace(shape, 0, 0, 1);

        Assert.That(result, Is.True);
        Assert.That(board.GetCell(0, 0), Is.EqualTo(1));
        Assert.That(board.GetCell(0, 1), Is.EqualTo(1));
        Assert.That(board.GetCell(0, 2), Is.EqualTo(1));
    }

    [Test]
    public void TryPlace_AtOffset_PlacesCorrectly()
    {
        var board = new BoardData(5, 5);
        // L 形:
        // X .
        // X .
        // X X
        var shape = new ShapeData(new bool[,]
        {
            { true, false },
            { true, false },
            { true, true }
        });

        bool result = board.TryPlace(shape, 1, 2, 3);

        Assert.That(result, Is.True);
        Assert.That(board.GetCell(1, 2), Is.EqualTo(3));
        Assert.That(board.GetCell(2, 2), Is.EqualTo(3));
        Assert.That(board.GetCell(3, 2), Is.EqualTo(3));
        Assert.That(board.GetCell(3, 3), Is.EqualTo(3));
        // false 位对应的格子应保持空
        Assert.That(board.GetCell(1, 3), Is.EqualTo(0));
        Assert.That(board.GetCell(2, 3), Is.EqualTo(0));
    }

    [Test]
    public void TryPlace_OutOfBounds_Fails_BoardUnchanged()
    {
        var board = new BoardData(3, 3);
        var shape = new ShapeData(new bool[,] { { true, true, true } });

        bool result = board.TryPlace(shape, 0, 2, 1); // 列 2,3,4 但棋盘只到 2

        Assert.That(result, Is.False);
        // 确保没有任何格子被修改
        for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            Assert.That(board.GetCell(r, c), Is.EqualTo(0));
    }

    [Test]
    public void TryPlace_NegativeOrigin_Fails()
    {
        var board = new BoardData(5, 5);
        var shape = new ShapeData(new bool[,] { { true } });

        Assert.That(board.TryPlace(shape, -1, 0, 1), Is.False);
        Assert.That(board.TryPlace(shape, 0, -1, 1), Is.False);
    }

    [Test]
    public void TryPlace_Collision_Fails_BoardUnchanged()
    {
        var board = new BoardData(5, 5);
        var shape1 = new ShapeData(new bool[,] { { true, true } });
        var shape2 = new ShapeData(new bool[,] { { true, true } });

        board.TryPlace(shape1, 0, 0, 1);
        bool result = board.TryPlace(shape2, 0, 1, 2); // 重叠于 (0,1)

        Assert.That(result, Is.False);
        // shape1 仍然完好
        Assert.That(board.GetCell(0, 0), Is.EqualTo(1));
        Assert.That(board.GetCell(0, 1), Is.EqualTo(1));
        // 未被覆盖
        Assert.That(board.GetCell(0, 2), Is.EqualTo(0));
    }

    [Test]
    public void TryPlace_AdjacentShapes_BothSucceed()
    {
        var board = new BoardData(5, 5);
        var shape1 = new ShapeData(new bool[,] { { true, true } });
        var shape2 = new ShapeData(new bool[,] { { true, true } });

        Assert.That(board.TryPlace(shape1, 0, 0, 1), Is.True);
        Assert.That(board.TryPlace(shape2, 0, 2, 2), Is.True);

        Assert.That(board.GetCell(0, 0), Is.EqualTo(1));
        Assert.That(board.GetCell(0, 1), Is.EqualTo(1));
        Assert.That(board.GetCell(0, 2), Is.EqualTo(2));
        Assert.That(board.GetCell(0, 3), Is.EqualTo(2));
    }

    [Test]
    public void TryPlace_FiresOnCellChanged_ForEachCell()
    {
        var board = new BoardData(5, 5);
        var shape = new ShapeData(new bool[,] { { true, true, true } });
        var events = new List<(int r, int c, int v)>();
        board.OnCellChanged += (r, c, v) => events.Add((r, c, v));

        board.TryPlace(shape, 1, 0, 5);

        Assert.That(events, Has.Count.EqualTo(3));
        Assert.That(events, Does.Contain((1, 0, 5)));
        Assert.That(events, Does.Contain((1, 1, 5)));
        Assert.That(events, Does.Contain((1, 2, 5)));
    }

    [Test]
    public void TryPlace_Failure_DoesNotFireEvents()
    {
        var board = new BoardData(3, 3);
        board.SetCell(0, 0, 1);
        var shape = new ShapeData(new bool[,] { { true, true } }); // 会撞到 (0,0)

        bool eventFired = false;
        board.OnCellChanged += (_, _, _) => eventFired = true;

        board.TryPlace(shape, 0, 0, 2);

        Assert.That(eventFired, Is.False);
    }

    [Test]
    public void TryPlace_NullShape_Throws()
    {
        var board = new BoardData(3, 3);
        Assert.Throws<ArgumentNullException>(() => board.TryPlace(null!, 0, 0, 1));
    }

    [Test]
    public void TryPlace_ZeroOrNegativeShapeId_Throws()
    {
        var board = new BoardData(3, 3);
        var shape = new ShapeData(new bool[,] { { true } });
        Assert.Throws<ArgumentOutOfRangeException>(() => board.TryPlace(shape, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.TryPlace(shape, 0, 0, -1));
    }

    // ─── Remove ─────────────────────────────────────────

    [Test]
    public void Remove_ClearsAllCellsWithGivenId()
    {
        var board = new BoardData(5, 5);
        var shape = new ShapeData(new bool[,]
        {
            { true, false },
            { true, true }
        });
        board.TryPlace(shape, 0, 0, 7);

        int removed = board.Remove(7);

        Assert.That(removed, Is.EqualTo(3));
        Assert.That(board.GetCell(0, 0), Is.EqualTo(0));
        Assert.That(board.GetCell(1, 0), Is.EqualTo(0));
        Assert.That(board.GetCell(1, 1), Is.EqualTo(0));
    }

    [Test]
    public void Remove_DoesNotAffectOtherShapes()
    {
        var board = new BoardData(5, 5);
        var s1 = new ShapeData(new bool[,] { { true, true } });
        var s2 = new ShapeData(new bool[,] { { true, true } });
        board.TryPlace(s1, 0, 0, 1);
        board.TryPlace(s2, 1, 0, 2);

        board.Remove(1);

        Assert.That(board.GetCell(0, 0), Is.EqualTo(0));
        Assert.That(board.GetCell(0, 1), Is.EqualTo(0));
        // shape 2 不受影响
        Assert.That(board.GetCell(1, 0), Is.EqualTo(2));
        Assert.That(board.GetCell(1, 1), Is.EqualTo(2));
    }

    [Test]
    public void Remove_NonExistentId_ReturnsZero()
    {
        var board = new BoardData(3, 3);
        int removed = board.Remove(99);
        Assert.That(removed, Is.EqualTo(0));
    }

    [Test]
    public void Remove_FiresOnCellChanged_ForEachRemovedCell()
    {
        var board = new BoardData(5, 5);
        var shape = new ShapeData(new bool[,] { { true, true } });
        board.TryPlace(shape, 0, 0, 3);

        var events = new List<(int r, int c, int v)>();
        board.OnCellChanged += (r, c, v) => events.Add((r, c, v));

        board.Remove(3);

        Assert.That(events, Has.Count.EqualTo(2));
        Assert.That(events, Does.Contain((0, 0, 0)));
        Assert.That(events, Does.Contain((0, 1, 0)));
    }

    [Test]
    public void Remove_ZeroOrNegativeId_Throws()
    {
        var board = new BoardData(3, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => board.Remove(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.Remove(-1));
    }

    // ─── CheckWinCondition ──────────────────────────────

    [Test]
    public void CheckWin_NoTarget_FullBoard_ReturnsTrue()
    {
        var board = new BoardData(2, 2);
        board.SetCell(0, 0, 1);
        board.SetCell(0, 1, 1);
        board.SetCell(1, 0, 2);
        board.SetCell(1, 1, 2);

        Assert.That(board.CheckWinCondition(), Is.True);
    }

    [Test]
    public void CheckWin_NoTarget_PartialBoard_ReturnsFalse()
    {
        var board = new BoardData(2, 2);
        board.SetCell(0, 0, 1);
        // (0,1), (1,0), (1,1) 仍为 0

        Assert.That(board.CheckWinCondition(), Is.False);
    }

    [Test]
    public void CheckWin_NoTarget_EmptyBoard_ReturnsFalse()
    {
        var board = new BoardData(2, 2);
        Assert.That(board.CheckWinCondition(), Is.False);
    }

    [Test]
    public void CheckWin_WithTarget_MatchingPattern_ReturnsTrue()
    {
        // 目标图案:
        // X .
        // . X
        var target = new bool[,]
        {
            { true, false },
            { false, true }
        };
        var board = new BoardData(2, 2, target);
        board.SetCell(0, 0, 1);
        board.SetCell(1, 1, 2);

        Assert.That(board.CheckWinCondition(), Is.True);
    }

    [Test]
    public void CheckWin_WithTarget_MissingCell_ReturnsFalse()
    {
        var target = new bool[,]
        {
            { true, true },
            { true, true }
        };
        var board = new BoardData(2, 2, target);
        board.SetCell(0, 0, 1);
        board.SetCell(0, 1, 1);
        board.SetCell(1, 0, 1);
        // (1,1) 缺失

        Assert.That(board.CheckWinCondition(), Is.False);
    }

    [Test]
    public void CheckWin_WithTarget_ExtraCell_ReturnsFalse()
    {
        // 目标只需填充 (0,0)
        var target = new bool[,]
        {
            { true, false },
            { false, false }
        };
        var board = new BoardData(2, 2, target);
        board.SetCell(0, 0, 1);
        board.SetCell(0, 1, 2); // 多余的格子

        Assert.That(board.CheckWinCondition(), Is.False);
    }

    [Test]
    public void CheckWin_WithTarget_AllEmpty_ReturnsTrue()
    {
        // 目标：全空（怪异但合法）
        var target = new bool[,]
        {
            { false, false },
            { false, false }
        };
        var board = new BoardData(2, 2, target);

        Assert.That(board.CheckWinCondition(), Is.True);
    }

    // ─── 集成场景 ───────────────────────────────────────

    [Test]
    public void Integration_PlaceAndRemove_ThenPlaceAgain()
    {
        var board = new BoardData(3, 3);
        var shape = new ShapeData(new bool[,] { { true, true } });

        board.TryPlace(shape, 0, 0, 1);
        board.Remove(1);

        // 移除后可再次放置
        bool result = board.TryPlace(shape, 0, 0, 2);
        Assert.That(result, Is.True);
        Assert.That(board.GetCell(0, 0), Is.EqualTo(2));
        Assert.That(board.GetCell(0, 1), Is.EqualTo(2));
    }

    [Test]
    public void Integration_SolvePuzzle_WithTarget()
    {
        // 3x3 目标图案：
        // X X .
        // X . .
        // . . .
        var target = new bool[,]
        {
            { true, true, false },
            { true, false, false },
            { false, false, false }
        };
        var board = new BoardData(3, 3, target);

        // L 形覆盖目标区域
        var lShape = new ShapeData(new bool[,]
        {
            { true, true },
            { true, false }
        });

        Assert.That(board.CheckWinCondition(), Is.False); // 还没放
        Assert.That(board.TryPlace(lShape, 0, 0, 1), Is.True);
        Assert.That(board.CheckWinCondition(), Is.True);  // 完美匹配
    }
}
