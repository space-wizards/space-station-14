using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The status effects display on the right side of the screen.
    /// </summary>
    public sealed class StatusEffectsUI : Control
    {
        public VBoxContainer VBox => _vBox;
        private readonly VBoxContainer _vBox;

        public StatusEffectsUI()
        {
            _vBox = new VBoxContainer {GrowHorizontal = GrowDirection.Begin};
            AddChild(_vBox);
            SetAnchorAndMarginPreset(LayoutPreset.TopRight);
            MarginTop = 250;
            MarginRight = 10;
        }
    }
}
