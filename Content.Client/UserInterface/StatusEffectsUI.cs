using Content.Client.UserInterface.Stylesheets;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The status effects display on the right side of the screen.
    /// </summary>
    public sealed class StatusEffectsUI : Control
    {
        public VBoxContainer VBox { get; }

        private PanelContainer _panelContainer;

        public StatusEffectsUI()
        {
            _panelContainer = new PanelContainer
            {
                StyleClasses = {StyleNano.StyleClassBorderedWindowPanel},
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            AddChild(_panelContainer);

            VBox = new VBoxContainer();
            _panelContainer.AddChild(VBox);

            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetMarginTop(this, 250);
        }
    }
}
