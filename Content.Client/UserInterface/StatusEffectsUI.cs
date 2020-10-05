using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The status effects display on the right side of the screen.
    /// </summary>
    public sealed class StatusEffectsUI : Control
    {
        public VBoxContainer VBox { get; }

        public StatusEffectsUI()
        {
            VBox = new VBoxContainer();
            AddChild(VBox);

            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetMarginTop(this, 250);
        }
    }
}
