# Roadmap: Picto Mino (数织拼图)

## 🏗️ 开发中 (In Progress)

## 📅 计划中 (Planned)

### Phase 5: 视觉表现 (Juice & Polish)
- [ ] **特效**
    - [ ] 方块放置时的缩放/弹力动画 (Tween)。
    - [ ] 消除/完成时的粒子特效。
    - [ ] 动态背景或光影效果。

### UI调整
- [ ] 增加标题与设置界面
- [ ] 标题界面
    - [ ] 选择关卡
    - [ ] 关卡编辑器
    - [ ] 设置
    - [ ] 退出
- [ ] 设置界面
    - [ ] 设置键盘与手柄的按键
- [ ] 选择关卡
    - [ ] 列出所有内置关卡选择进入
    - [ ] 打开外部关卡文件（通过打开文件选择`.level`关卡文件）
- [ ] 关卡编辑器
    - [ ] 选择游戏盘大小（最大25x25）
    - [ ] 编辑数织图形
    - [ ] 检查数织是否唯一解
    - [ ] 选择使用的形状
    - [ ] 摆放形状
    - [ ] 检查解的唯一性
    - [ ] 导出关卡文件（可以导出总的.level文件或是不打包导出各文件到目录）

## ✅ 已完成 (Completed)
- [x] 确定游戏名称：Picto Mino (数织拼图)
- [x] 确定技术栈：Godot 4.x (C#), MVVM 架构
- [x] 制定 Copilot 开发准则
- [x] **项目初始化与基础架构**
    - [x] 建立文件夹结构 (`Scripts/Core`, `Scripts/View`, `Tests`, `Scenes`)
    - [x] 配置 C# 测试环境 (NUnit + 独立 Core 类库)
    - [x] 设立基础 Git 仓库与 `.gitignore`

### Phase 1: 核心逻辑 (Pure C# TDD)
- [x] **数据结构定义**
    - [x] `ShapeData` 类：定义多格骨牌的形状（宽、高、矩阵）。
    - [x] `BoardData` 类：定义棋盘网格状态（int[,] 数组）。
- [x] **核心规则实现**
    - [x] 实现 `TryPlace(shape, x, y)`：边界检查与碰撞检测。
    - [x] 实现 `Remove(x, y)`：移除方块逻辑。
    - [x] 实现 `CheckWinCondition()`：判断是否填满或符合目标。
- [x] **事件系统**
    - [x] 实现 `OnCellChanged` 事件，供 View 层订阅。

### Phase 2: 渲染与交互基础 (Godot Integration)
- [x] **棋盘渲染 (View)**
    - [x] 制作基础 TileSet (方块、空格、背景)。
    - [x] 实现 `BoardView.cs`：订阅 `OnCellChanged` 并更新 `TileMapLayer`。
- [x] **输入系统 (Controller)**
    - [x] 配置 Godot Input Map (WASD, 方向键, 鼠标点击)。
    - [x] 实现坐标转换：`GlobalPosition` <-> `GridCoordinate`。
    - [x] **双模输入策略 (Ghost Hand)**
        - [x] 实现 `MouseStrategy`：Ghost 跟随鼠标实时移动。
        - [x] 实现 `GamepadStrategy`：Ghost 响应离散方向键。
        - [x] 实现 `InputDirector`：在不同输入设备间自动切换策略。

### Phase 3: 算法与关卡生成 (DLX)
- [x] **DLX 算法移植**
    - [x] 实现 Dancing Links 基础数据结构 (Node, Column)。
    - [x] 实现精确覆盖问题 (Exact Cover) 求解器。
- [x] **关卡生成器**
    - [x] 编写工具：将当前棋盘状态导出为 DLX 矩阵。
    - [x] 编写生成器：随机生成可解的拼图谜题。

### Phase 4: UI 与 游戏流程
- [x] **侧边栏 (Palette)**
    - [x] UI 实现：显示当前可用方块列表。
    - [x] 交互逻辑：从侧边栏 "拿起" 方块进入放置模式。
- [x] **游戏循环**
    - [x] 胜利结算界面。
    - [x] 关卡选择菜单。

- [x] 关卡调整
    - [x] 关卡信息保存为统一的 `.level` 文件
        - [x] `.level` 文件内容为相应的关卡文件zip压缩后改后缀
        - [x] 内部内容如下：
            - [x] `level.json`，关卡数据
            - [x] `*.shape.json`，每个`.shape.json`定义一个自定义形状
            - [x] `metadata.json`，元数据文件，指定如版本信息（目前这个为版本1，为以后格式破坏性升级兼容用）、颜色索引、shape的索引（索引内置形状和自定义形状）等信息，以后有什么要新增的元数据也加这里
    - [x] 内置关卡保存在内置资源中 `res://Levels`
        - [x] `res://Levels/index.json` 为关卡索引，按顺序在Levels下取相应的 `.level` 关卡文件
    - [x] 内置形状
        - [x] `res://Shapes` 下放一些内置的形状，每个形状为一个 `.shape.json` 文件，为 `ShapeData` 序列化保存