using Content.Client.UserInterface.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

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
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Top
            };
            AddChild(panelContainer);

            Grid = new GridContainer
            {
                MaxGridHeight = 64,
                ExpandBackwards = true
            };
            panelContainer.AddChild(Grid);

            MinSize = (64, 64);
        }

        // This makes no sense but I'm leaving it in place in case I break anything by removing it.

        protected override void Resized()
        {
            // TODO: Can rework this once https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
            // this is here because there isn't currently a good way to allow the grid to adjust its height based
            // on constraints, otherwise we would use anchors to lay it out
            base.Resized();
            Grid.MaxGridHeight = Height;
        }

        protected override void UIScaleChanged()
        {
            Grid.MaxGridHeight = Height;
            base.UIScaleChanged();
        }
    }
}
