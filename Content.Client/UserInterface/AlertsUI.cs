using Content.Client.UserInterface.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The status effects display on the right side of the screen.
    /// </summary>
    public sealed class AlertsUI : Control
    {
        public GridContainer Grid { get; }

        public AlertsUI()
        {
            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetGrowVertical(this, LayoutContainer.GrowDirection.End);
            LayoutContainer.SetAnchorTop(this, 0f);
            LayoutContainer.SetAnchorRight(this, 1f);
            LayoutContainer.SetAnchorBottom(this, 1f);
            LayoutContainer.SetMarginBottom(this, -180);
            LayoutContainer.SetMarginTop(this, 250);
            LayoutContainer.SetMarginRight(this, -10);
            var panelContainer = new PanelContainer
            {
                StyleClasses = {StyleNano.StyleClassTransparentBorderedWindowPanel},
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.None
            };
            AddChild(panelContainer);

            Grid = new GridContainer
            {
                MaxHeight = 64,
                ExpandBackwards = true
            };
            panelContainer.AddChild(Grid);
        }

        protected override void Resized()
        {
            // TODO: Can rework this once https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
            // this is here because there isn't currently a good way to allow the grid to adjust its height based
            // on constraints, otherwise we would use anchors to lay it out
            base.Resized();
            Grid.MaxHeight = Height;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            // allows us to shrink down to a single row
            return (64, 64);
        }

        protected override void UIScaleChanged()
        {
            Grid.MaxHeight = Height;
            base.UIScaleChanged();
        }
    }
}
