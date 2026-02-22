namespace PictoMino.Core;

/// <summary>
/// 侧边栏数据。管理当前关卡可用的形状列表及其选择状态。
/// </summary>
public class PaletteData
{
    private readonly List<ShapeData> _shapes;
    private readonly List<bool> _usedStates;
    private int _selectedIndex = -1;

    /// <summary>
    /// 可用形状列表（只读）。
    /// </summary>
    public IReadOnlyList<ShapeData> Shapes => _shapes;

    /// <summary>
    /// 当前选中的形状索引。-1 表示未选中任何形状。
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        private set
        {
            if (_selectedIndex == value) return;
            int oldIndex = _selectedIndex;
            _selectedIndex = value;
            OnSelectionChanged?.Invoke(oldIndex, value);
        }
    }

    /// <summary>
    /// 当前选中的形状。
    /// </summary>
    public ShapeData? SelectedShape => _selectedIndex >= 0 && _selectedIndex < _shapes.Count
        ? _shapes[_selectedIndex]
        : null;

    /// <summary>
    /// 剩余未使用的形状数量。
    /// </summary>
    public int RemainingCount => _usedStates.Count(used => !used);

    /// <summary>
    /// 当选中项变化时触发。参数为 (oldIndex, newIndex)。
    /// </summary>
    public event Action<int, int>? OnSelectionChanged;

    /// <summary>
    /// 当形状使用状态变化时触发。参数为 (index, isUsed)。
    /// </summary>
    public event Action<int, bool>? OnShapeUsedChanged;

    /// <summary>
    /// 当所有形状都已使用时触发。
    /// </summary>
    public event Action? OnAllShapesUsed;

    public PaletteData(IEnumerable<ShapeData> shapes)
    {
        ArgumentNullException.ThrowIfNull(shapes);
        _shapes = shapes.ToList();
        _usedStates = Enumerable.Repeat(false, _shapes.Count).ToList();
    }

    /// <summary>
    /// 选中指定索引的形状。
    /// </summary>
    /// <param name="index">形状索引，-1 表示取消选择</param>
    /// <returns>选择是否成功</returns>
    public bool Select(int index)
    {
        if (index < -1 || index >= _shapes.Count) return false;
        if (index >= 0 && _usedStates[index]) return false; // 已使用的不能选
        
        SelectedIndex = index;
        return true;
    }

    /// <summary>
    /// 取消当前选择。
    /// </summary>
    public void Deselect()
    {
        SelectedIndex = -1;
    }

    /// <summary>
    /// 检查指定索引的形状是否已被使用。
    /// </summary>
    public bool IsUsed(int index)
    {
        if (index < 0 || index >= _shapes.Count) return false;
        return _usedStates[index];
    }

    /// <summary>
    /// 标记当前选中的形状为已使用，并自动取消选择。
    /// </summary>
    /// <returns>是否成功标记</returns>
    public bool MarkSelectedAsUsed()
    {
        if (_selectedIndex < 0) return false;
        
        int index = _selectedIndex;
        _usedStates[index] = true;
        SelectedIndex = -1;
        
        OnShapeUsedChanged?.Invoke(index, true);
        
        if (RemainingCount == 0)
        {
            OnAllShapesUsed?.Invoke();
        }
        
        return true;
    }

    /// <summary>
    /// 将指定索引的形状标记为未使用。
    /// </summary>
    public bool MarkAsUnused(int index)
    {
        if (index < 0 || index >= _shapes.Count) return false;
        if (!_usedStates[index]) return false;
        
        _usedStates[index] = false;
        OnShapeUsedChanged?.Invoke(index, false);
        return true;
    }

    /// <summary>
    /// 重置所有形状为未使用状态。
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < _usedStates.Count; i++)
        {
            if (_usedStates[i])
            {
                _usedStates[i] = false;
                OnShapeUsedChanged?.Invoke(i, false);
            }
        }
        SelectedIndex = -1;
    }

    /// <summary>
    /// 选择下一个可用的形状。
    /// </summary>
    /// <returns>是否成功选择</returns>
    public bool SelectNext()
    {
        int start = _selectedIndex + 1;
        for (int i = 0; i < _shapes.Count; i++)
        {
            int idx = (start + i) % _shapes.Count;
            if (!_usedStates[idx])
            {
                SelectedIndex = idx;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 选择上一个可用的形状。
    /// </summary>
    /// <returns>是否成功选择</returns>
    public bool SelectPrevious()
    {
        int start = _selectedIndex < 0 ? _shapes.Count - 1 : _selectedIndex - 1;
        for (int i = 0; i < _shapes.Count; i++)
        {
            int idx = (start - i + _shapes.Count) % _shapes.Count;
            if (!_usedStates[idx])
            {
                SelectedIndex = idx;
                return true;
            }
        }
        return false;
    }
}
