using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The status effects display on the right side of the screen.
    /// </summary>
    public sealed class StatusEffectsUI : Control
    {
        private readonly VBoxContainer _vBox;

        private TextureRect _healthStatusRect;

        public StatusEffectsUI()
        {
            _vBox = new VBoxContainer {GrowHorizontal = GrowDirection.Begin};
            AddChild(_vBox);

            _vBox.AddChild(_healthStatusRect = new TextureRect
            {
                TextureScale = (2, 2),
                Texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Mob/UI/Human/human0.png")
            });

            SetAnchorAndMarginPreset(LayoutPreset.TopRight);
            MarginTop = 250;
            MarginRight = 10;
        }

        public void SetHealthIcon(Texture texture)
        {
            _healthStatusRect.Texture = texture;
        }
    }
}
