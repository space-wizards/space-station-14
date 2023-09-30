using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.Client.UserInterface.Controls;

public sealed class ListContainer : Control
{
    public const string StylePropertySeparation = "separation";
    public const string StyleClassListContainerButton = "list-container-button";

    public int? SeparationOverride { get; set; }

    public bool Group
    {
        get => _buttonGroup != null;
        set => _buttonGroup = value ? new ButtonGroup() : null;
    }
    public bool Toggle { get; set; }
    public Action<ListData, ListContainerButton>? GenerateItem;
    public Action<BaseButton.ButtonEventArgs, ListData>? ItemPressed;
    public IReadOnlyList<ListData> Data => _data;

    private const int DefaultSeparation = 3;

    private readonly VScrollBar _vScrollBar;
    private readonly Dictionary<ListData, ListContainerButton> _buttons = new();

    private List<ListData> _data = new();
    private ListData? _selected;
    private float _itemHeight = 0;
    private float _totalHeight = 0;
    private int _topIndex = 0;
    private int _bottomIndex = 0;
    private bool _updateChildren = false;
    private bool _suppressScrollValueChanged;
    private ButtonGroup? _buttonGroup;

    public int ScrollSpeedY { get; set; } = 50;

    private int ActualSeparation
    {
        get
        {
            if (TryGetStyleProperty(StylePropertySeparation, out int separation))
            {
                return separation;
            }

            return SeparationOverride ?? DefaultSeparation;
        }
    }

    public ListContainer()
    {
        HorizontalExpand = true;
        VerticalExpand = true;
        RectClipContent = true;
        MouseFilter = MouseFilterMode.Pass;

        _vScrollBar = new VScrollBar
        {
            HorizontalExpand = false,
            HorizontalAlignment = HAlignment.Right
        };
        AddChild(_vScrollBar);
        _vScrollBar.OnValueChanged += ScrollValueChanged;
    }

    public void PopulateList(IReadOnlyList<ListData> data)
    {
        if ((_itemHeight == 0 || _data is {Count: 0}) && data.Count > 0)
        {
            ListContainerButton control = new(data[0]);
            GenerateItem?.Invoke(data[0], control);
            control.Measure(Vector2Helpers.Infinity);
            _itemHeight = control.DesiredSize.Y;
            control.Dispose();
        }

        // Ensure buttons are re-generated.
        foreach (var button in _buttons.Values)
        {
            button.Dispose();
        }
        _buttons.Clear();

        _data = data.ToList();
        _updateChildren = true;
        InvalidateArrange();
    }

    public void DirtyList()
    {
        _updateChildren = true;
        InvalidateArrange();
    }

    #region Selection

    public void Select(ListData data)
    {
        if (!_data.Contains(data))
            return;
        if (_buttons.TryGetValue(data, out var button) && Toggle)
            button.Pressed = true;
        _selected = data;
        button ??= new ListContainerButton(data);
        OnItemPressed(new BaseButton.ButtonEventArgs(button,
            new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up,
                new ScreenCoordinates(0, 0, WindowId.Main), true, Vector2.Zero, Vector2.Zero)));
    }

    /*
     * Need to implement selecting the first item in code.
     * Need to implement updating one entry without having to repopulate
     */
    #endregion

    private void OnItemPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Button is not ListContainerButton button)
            return;
        _selected = button.Data;
        ItemPressed?.Invoke(args, button.Data);
    }

    [Pure]
    private Vector2 GetScrollValue()
    {
        var v = _vScrollBar.Value;
        if (!_vScrollBar.Visible)
        {
            v = 0;
        }
        return new Vector2(0, v);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        #region Scroll
        var cHeight = _totalHeight;
        var vBarSize = _vScrollBar.DesiredSize.X;
        var (finalWidth, finalHeight) = finalSize;

        try
        {
            // Suppress events to avoid weird recursion.
            _suppressScrollValueChanged = true;

            if (finalHeight < cHeight)
                finalWidth -= vBarSize;

            if (finalHeight < cHeight)
            {
                _vScrollBar.Visible = true;
                _vScrollBar.Page = finalHeight;
                _vScrollBar.MaxValue = cHeight;
            }
            else
                _vScrollBar.Visible = false;
        }
        finally
        {
            _suppressScrollValueChanged = false;
        }

        if (_vScrollBar.Visible)
        {
            _vScrollBar.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));
        }
        #endregion

        #region Rebuild Children
        /*
         * Example:
         *
         * var _itemHeight = 32;
         * var separation = 3;
         *  32 | 32 | Control.Size.Y 0
         *  35 |  3 | Padding
         *  67 | 32 | Control.Size.Y 1
         *  70 |  3 | Padding
         * 102 | 32 | Control.Size.Y 2
         * 105 |  3 | Padding
         * 137 | 32 | Control.Size.Y 3
         *
         * If viewport height is 60
         * visible should be 2 items (start = 0, end = 1)
         *
         * scroll.Y = 11
         * visible should be 3 items (start = 0, end = 2)
         *
         * start expected: 11 (item: 0)
         * var start = (int) (scroll.Y
         *
         * if (scroll == 32) then { start = 1 }
         * var start = (int) (scroll.Y + separation / (_itemHeight + separation));
         * var start = (int) (32 + 3 / (32 + 3));
         * var start = (int) (35 / 35);
         * var start = (int) (1);
         *
         * scroll = 0, height = 36
         * if (scroll + height == 36) then { end = 2 }
         * var end = (int) Math.Ceiling(scroll.Y + height / (_itemHeight + separation));
         * var end = (int) Math.Ceiling(0 + 36 / (32 + 3));
         * var end = (int) Math.Ceiling(36 / 35);
         * var end = (int) Math.Ceiling(1.02857);
         * var end = (int) 2;
         *
         */
        var scroll = GetScrollValue();
        var oldTopIndex = _topIndex;
        _topIndex = (int) ((scroll.Y + ActualSeparation) / (_itemHeight + ActualSeparation));
        if (_topIndex != oldTopIndex)
            _updateChildren = true;

        var oldBottomIndex = _bottomIndex;
        _bottomIndex = (int) Math.Ceiling((scroll.Y + finalHeight) / (_itemHeight + ActualSeparation));
        _bottomIndex = Math.Min(_bottomIndex, _data.Count);
        if (_bottomIndex != oldBottomIndex)
            _updateChildren = true;

        // When scrolling only rebuild visible list when a new item should be visible
        if (_updateChildren)
        {
            _updateChildren = false;

            var toRemove = new Dictionary<ListData, ListContainerButton>(_buttons);
            foreach (var child in Children.ToArray())
            {
                if (child == _vScrollBar)
                    continue;
                RemoveChild(child);
            }

            if (_data.Count > 0)
            {
                for (var i = _topIndex; i < _bottomIndex; i++)
                {
                    var data = _data[i];

                    if (_buttons.TryGetValue(data, out var button))
                        toRemove.Remove(data);
                    else
                    {
                        button = new ListContainerButton(data);
                        button.OnPressed += OnItemPressed;
                        button.ToggleMode = Toggle;
                        button.Group = _buttonGroup;

                        GenerateItem?.Invoke(data, button);
                        _buttons.Add(data, button);

                        if (Toggle && data == _selected)
                            button.Pressed = true;
                    }
                    AddChild(button);
                    button.Measure(finalSize);
                }
            }

            foreach (var (data, button) in toRemove)
            {
                _buttons.Remove(data);
                button.Dispose();
            }

            _vScrollBar.SetPositionLast();
        }
        #endregion

        #region Layout Children
        // Use pixel position
        var pixelWidth = (int)(finalWidth * UIScale);
        var pixelSeparation = (int) (ActualSeparation * UIScale);

        var pixelOffset = (int) -((scroll.Y - _topIndex * (_itemHeight + ActualSeparation)) * UIScale);
        var first = true;
        foreach (var child in Children)
        {
            if (child == _vScrollBar)
                continue;
            if (!first)
                pixelOffset += pixelSeparation;
            first = false;

            var pixelSize = child.DesiredPixelSize.Y;
            var targetBox = new UIBox2i(0, pixelOffset, pixelWidth, pixelOffset + pixelSize);
            child.ArrangePixel(targetBox);

            pixelOffset += pixelSize;
        }
        #endregion

        return finalSize;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        _vScrollBar.Measure(availableSize);
        availableSize.X -= _vScrollBar.DesiredSize.X;

        var constraint = new Vector2(availableSize.X, float.PositiveInfinity);

        var childSize = Vector2.Zero;
        foreach (var child in Children)
        {
            child.Measure(constraint);
            if (child == _vScrollBar)
                continue;
            childSize = Vector2.Max(childSize, child.DesiredSize);
        }

        if (_itemHeight == 0 && childSize.Y != 0)
            _itemHeight = childSize.Y;

        _totalHeight = _itemHeight * _data.Count + ActualSeparation * (_data.Count - 1);

        return new Vector2(childSize.X, 0);
    }

    private void ScrollValueChanged(Robust.Client.UserInterface.Controls.Range _)
    {
        if (_suppressScrollValueChanged)
        {
            return;
        }

        InvalidateArrange();
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        _vScrollBar.ValueTarget -= args.Delta.Y * ScrollSpeedY;

        args.Handle();
    }
}

public sealed class ListContainerButton : ContainerButton, IEntityControl
{
    public readonly ListData Data;
    // public PanelContainer Background;

    public ListContainerButton(ListData data)
    {
        Data = data;
        // AddChild(Background = new PanelContainer
        // {
        //     HorizontalExpand = true,
        //     VerticalExpand = true,
        //     PanelOverride = new StyleBoxFlat {BackgroundColor = new Color(55, 55, 68)}
        // });
    }

    public EntityUid? UiEntity => (Data as EntityListData)?.Uid;
}

#region Data
public abstract record ListData;

public record EntityListData(EntityUid Uid) : ListData;
#endregion
