# Picto Mino (数织拼图) - Copilot Instructions

## Game Concept

**Picto Mino** is a hybrid puzzle game that combines the logic of **Nonograms** (Picross) with the spatial reasoning of **Polyominoes** (Tetris-like blocks).
- **Core Loop:** Instead of filling cells one by one (like traditional Nonograms), players must place **pre-defined Polyomino shapes** onto the grid to satisfy the row/column constraints.
- **Goal:** Correctly place all available shapes to reveal a hidden pixel-art image.
- **Vibe:** A chill, "juicy" puzzle experience with satisfying feedback, smooth animations, and a clean, modern aesthetic.

## Key Mechanics

1.  **Constraint Satisfaction:** Rows and columns have numbers (e.g., "3 1") indicating the lengths of filled block groups.
2.  **Shape Inventory:** Players are given a specific set of Polyominoes (e.g., L-shape, T-shape) that must ALL be used to solve the puzzle.
3.  **Dual Input Support:** Seamlessly supports both Mouse (Drag & Drop) and Gamepad/Keyboard (Grid Cursor Navigation).

## 1. Project Philosophy & Architecture
**Core Principle: Strict Logic/View Separation**
- This project uses a **MVVM-like architecture**.
- **Model (Core Logic):** Pure C# classes. NEVER reference `Godot` namespaces here. Must be testable via NUnit/xUnit without the Godot Editor running.
- **View (Godot):** `Node`, `TileMapLayer`, `Sprite2D`. Responsible ONLY for rendering state and capturing raw input.
- **ViewModel/Controller:** Bridges the View and Model. Converts Godot Input Events into Model commands.

## 2. Directory Structure
Follow this structure strictly:
- `Scripts/Core/` : **PURE C# ONLY**. Game rules, Board state, DLX Algorithm, Shape data. NO `using Godot;`.
- `Scripts/View/` : Godot-specific scripts (Monobehaviors) attached to Nodes.
- `Scripts/Input/` : Input arbitration (Mouse vs Gamepad strategies).
- `Tests/` : Unit tests (referencing `Scripts/Core`).
- `Scenes/` : Godot `.tscn` files.
- `Assets/` : Art, Audio, Resources.
- `ROADMAP.md` : High-level project roadmap and milestones.禁止修改此文件的内容。

## 3. Development Workflow (TDD)
1.  **Red:** Write a failing test in `Tests/` describing the desired logic (e.g., "Placing a block on an occupied cell should return false").
2.  **Green:** Implement the minimal code in `Scripts/Core/` to pass the test.
3.  **Refactor:** Optimize the code.
4.  **Integrate:** Only after the logic is solid, create/update the Godot Scene to visualize it.

## 4. Key Algorithms & Features
- **Dancing Links (DLX):** Used for level generation and solving Nonogram/Polyomino constraints.
- **Ghost Hand System:**
  - **Mouse/Touch:** Direct manipulation (Drag & Drop). Ghost follows cursor.
  - **Gamepad/Keyboard:** Discrete cursor movement. Ghost moves step-by-step.
  - **State Machine:** Distinct states for `Selecting` (Palette) and `Placing` (Board).

## 5. Git Commit Standards
- **Rule:** Do NOT commit automatically. Only generate commit messages when explicitly requested.
- **Format:** `Emoji Type: Summary`
  - If changes are complex, use a multi-line format.
- **Emojis:**
  - ✨ `feat`: New feature
  - 🐛 `fix`: Bug fix
  - 📝 `docs`: Documentation
  - ♻️ `refactor`: Code restructuring without logic change
  - ✅ `test`: Adding/Editing tests
  - 🎨 `style`: Formatting/UI tweaks

## 6. Godot Implementation Details
- Use `TileMapLayer` (Godot 4.3+) for the grid rendering.
- Use `Signal` (C# Events) to notify the View when the Model changes.
- Avoid using `GetNode()` strings repeatedly; export typed fields (e.g., `[Export] private TileMapLayer _grid;`).
- **Input Handling:** Use `_UnhandledInput` for gameplay logic. Use the "Input Map" names defined in Project Settings (e.g., `cursor_up`, `interact_main`).
