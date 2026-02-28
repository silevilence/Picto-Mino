# Picto Mino (数织拼图) - Copilot Instructions

## Game Concept

**Picto Mino** 是一款结合 **数织 (Nonograms/Picross)** 逻辑与 **多格骨牌 (Polyominoes)** 空间推理的混合谜题游戏。
- **核心玩法:** 玩家需要将预定义的多格骨牌形状放置到网格上，使其满足行/列数字约束。
- **目标:** 正确放置所有形状，揭示隐藏的像素画图案。

## Architecture

### 严格的 Model/View 分离
```
Scripts/Core/     ← 纯 C#，命名空间 PictoMino.Core，禁止 using Godot
Scripts/View/     ← Godot 节点，命名空间 PictoMino.View
Scripts/Input/    ← 输入策略 (Mouse/Gamepad)
Tests/            ← NUnit 测试，仅测试 Core 层
```

**关键原则:** `Scripts/Core/` 必须可独立编译和测试，无任何 Godot 依赖。

### 事件驱动模式
View 通过属性 setter 订阅 Model 事件：
```csharp
// BoardView.cs 示例
public BoardData? BoardData
{
    set {
        if (_boardData != null) _boardData.OnCellChanged -= OnCellChanged;
        _boardData = value;
        if (_boardData != null) _boardData.OnCellChanged += OnCellChanged;
    }
}
```

## Code Style

### 命名约定
```csharp
private readonly int[,] _cells;        // 私有字段: _camelCase
public int Rows { get; }               // 公共属性: PascalCase
public bool TryPlace(...)              // 方法: PascalCase
public event Action<int, int>? OnCellChanged;  // 事件: On 前缀
```

### Godot 特有
```csharp
public partial class BoardView : Node2D    // 必须使用 partial
[Export] public int CellSize { get; set; } = 32;
_boardView = GetNodeOrNull<BoardView>("%BoardView");  // % = UniqueNameInOwner
```

### 文档注释
使用中文 XML 文档：
```csharp
/// <summary>棋盘网格状态。0 = 空格，正整数 = 被对应 ID 的方块占据。</summary>
```

## Build and Test

```powershell
# 构建核心库 (纯 C#)
dotnet build Scripts/Core/PictoMino.Core.csproj

# 运行所有测试
dotnet test Tests/PictoMino.Tests.csproj

# 运行特定测试
dotnet test Tests/PictoMino.Tests.csproj --filter "FullyQualifiedName~BoardDataTests"

# 带覆盖率测试
dotnet test Tests/PictoMino.Tests.csproj --collect:"XPlat Code Coverage"
```

## Testing Patterns

使用 NUnit 3.x，遵循 Arrange-Act-Assert：
```csharp
[Test]
public void MethodUnderTest_Scenario_ExpectedBehavior()
{
    var board = new BoardData(5, 5);
    bool result = board.TryPlace(shape, 0, 0, 1);
    Assert.That(result, Is.True);
}
```

**事件测试:**
```csharp
board.OnCellChanged += (r, c, v) => { eventRow = r; eventCol = c; };
board.SetCell(0, 1, 7);
Assert.That(eventRow, Is.EqualTo(0));
```

## Key Components

| 文件 | 职责 |
|------|------|
| [BoardData.cs](Scripts/Core/BoardData.cs) | 棋盘状态，放置/移除逻辑 |
| [ShapeData.cs](Scripts/Core/ShapeData.cs) | 多格骨牌形状定义，旋转 |
| [ExactCoverSolver.cs](Scripts/Core/DLX/ExactCoverSolver.cs) | DLX 算法求解器 |
| [PuzzleGenerator.cs](Scripts/Core/DLX/PuzzleGenerator.cs) | 谜题生成 |
| [BoardView.cs](Scripts/View/BoardView.cs) | 棋盘渲染 (TileMapLayer) |
| [GameController.cs](Scripts/View/GameController.cs) | 游戏流程协调 |
| [InputDirector.cs](Scripts/Input/InputDirector.cs) | 输入设备自动切换 |

## Conventions

- **坐标系:** 使用 `(row, col)` 顺序，row 对应 Y 轴
- **形状 ID:** 正整数表示占据，0 表示空格
- **TDD:** 先写测试 → 实现 Core → 最后集成 View
- **ROADMAP.md:** 只读，禁止Agent修改，只允许人工更新，但提交时要一起提交

## Git Commits

仅在明确要求时生成提交消息，格式：`Emoji Type: Summary`
- ✨ `feat` | 🐛 `fix` | 📝 `docs` | ♻️ `refactor` | ✅ `test` | 🎨 `style`
- 可选的详细描述