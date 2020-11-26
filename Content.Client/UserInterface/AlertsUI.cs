using System;
using Content.Client.UserInterface.Stylesheets;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The status effects display on the right side of the screen.
    /// </summary>
    public sealed class AlertsUI : Control
    {
        public GridContainer Grid { get; }

        private readonly IClyde _clyde;

        public AlertsUI(IClyde clyde)
        {
            _clyde = clyde;
            var panelContainer = new PanelContainer
            {
                StyleClasses = {StyleNano.StyleClassTransparentBorderedWindowPanel},
                SizeFlagsVertical = SizeFlags.FillExpand,
            };
            AddChild(panelContainer);

            Grid = new GridContainer
            {
                MaxHeight = CalcMaxHeight(clyde.ScreenSize),
                ExpandBackwards = true
            };
            panelContainer.AddChild(Grid);
            clyde.OnWindowResized += ClydeOnOnWindowResized;

            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetMarginTop(this, 250);
        }

        protected override void UIScaleChanged()
        {
            Grid.MaxHeight = CalcMaxHeight(_clyde.ScreenSize);
            base.UIScaleChanged();
        }

        private void ClydeOnOnWindowResized(WindowResizedEventArgs obj)
        {
            // TODO: Can rework this once https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
            // this is here because there isn't currently a good way to allow the grid to adjust its height based
            // on constraints, otherwise we would use anchors to lay it out
            Grid.MaxHeight = CalcMaxHeight(obj.NewSize);;
        }

        private float CalcMaxHeight(Vector2i screenSize)
        {
            return Math.Max(((screenSize.Y) / UIScale) - 420, 1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _clyde.OnWindowResized -= ClydeOnOnWindowResized;
            }
        }
    }
}
