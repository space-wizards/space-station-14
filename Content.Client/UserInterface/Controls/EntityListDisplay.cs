using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Stylesheets;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls
{
    public sealed class EntityListDisplay : Control
    {
        public const string StylePropertySeparation = "separation";

        public int? SeparationOverride { get; set; }
        public Action<EntityUid, EntityContainerButton>? GenerateItem;
        public Action<BaseButton.ButtonEventArgs, EntityUid>? ItemPressed;

        private const int DefaultSeparation = 3;

        private readonly VScrollBar _vScrollBar;

        private List<EntityUid>? _entityUids;
        private int _count = 0;
        private float _itemHeight = 0;
        private float _totalHeight = 0;
        private int _topIndex = 0;
        private int _bottomIndex = 0;
        private bool _updateChildren = false;
        private bool _suppressScrollValueChanged;

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

        public EntityListDisplay()
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

        public void PopulateList(List<EntityUid> entities)
        {
            if (_count == 0 && entities.Count > 0)
            {
                EntityContainerButton control = new(entities[0]);
                GenerateItem?.Invoke(entities[0], control);
                control.Measure(Vector2.Infinity);
                _itemHeight = control.DesiredSize.Y;
                control.Dispose();
            }
            _count = entities.Count;
            _entityUids = entities;
            _updateChildren = true;
            InvalidateArrange();
        }

        private void OnItemPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Button is not EntityContainerButton button)
                return;
            ItemPressed?.Invoke(args, button.EntityUid);
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
            var separation = (int) (ActualSeparation * UIScale);

            #region Scroll
            var cHeight = _totalHeight;
            var vBarSize = _vScrollBar.DesiredSize.X;
            var (sWidth, sHeight) = finalSize;

            try
            {
                // Suppress events to avoid weird recursion.
                _suppressScrollValueChanged = true;

                if (sHeight < cHeight)
                    sWidth -= vBarSize;

                if (sHeight < cHeight)
                {
                    _vScrollBar.Visible = true;
                    _vScrollBar.Page = sHeight;
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
            _topIndex = (int) ((scroll.Y + separation) / (_itemHeight + separation));
            if (_topIndex != oldTopIndex)
                _updateChildren = true;

            var oldBottomIndex = _bottomIndex;
            _bottomIndex = (int) Math.Ceiling((scroll.Y + Height) / (_itemHeight + separation));
            _bottomIndex = Math.Min(_bottomIndex, _count);
            if (_bottomIndex != oldBottomIndex)
                _updateChildren = true;

            // When scrolling only rebuild visible list when a new item should be visible
            if (_updateChildren)
            {
                _updateChildren = false;

                foreach (var child in Children.ToArray())
                {
                    if (child == _vScrollBar)
                        continue;
                    RemoveChild(child);
                }

                if (_entityUids != null)
                {
                    for (var i = _topIndex; i < _bottomIndex; i++)
                    {
                        var entity = _entityUids[i];

                        var button = new EntityContainerButton(entity);
                        button.OnPressed += OnItemPressed;

                        GenerateItem?.Invoke(entity, button);
                        AddChild(button);
                    }
                }

                _vScrollBar.SetPositionLast();
            }
            #endregion

            #region Layout Children
            // Use pixel position
            var pixelWidth = (int)(sWidth * UIScale);

            var offset = (int) -((scroll.Y - _topIndex * (_itemHeight + separation)) * UIScale);
            var first = true;
            foreach (var child in Children)
            {
                if (child == _vScrollBar)
                    continue;
                if (!first)
                    offset += separation;
                first = false;

                var size = child.DesiredPixelSize.Y;
                var targetBox = new UIBox2i(0, offset, pixelWidth, offset + size);
                child.ArrangePixel(targetBox);

                offset += size;
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
                childSize = Vector2.ComponentMax(childSize, child.DesiredSize);
            }

            _totalHeight = childSize.Y * _count + ActualSeparation * (_count - 1);

            return new Vector2(childSize.X, 0f);
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

    public sealed class EntityContainerButton : ContainerButton
    {
        public EntityUid EntityUid;

        public EntityContainerButton(EntityUid entityUid)
        {
            EntityUid = entityUid;
            AddStyleClass(StyleNano.StyleClassStorageButton);
        }
    }
}
