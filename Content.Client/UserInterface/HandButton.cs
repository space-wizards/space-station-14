using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface
{
    public class HandButton : ItemSlotButton
    {
        private bool _activeHand;
        private bool _highlighted;

        public HandButton(Texture texture, Texture storageTexture, string textureName, Texture blockedTexture, HandLocation location) : base(texture, storageTexture, textureName)
        {
            Location = location;

            AddChild(Blocked = new TextureRect
            {
                Texture = blockedTexture,
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });
        }

        public HandLocation Location { get; }
        public TextureRect Blocked { get; }

        public void SetActiveHand(bool active)
        {
            _activeHand = active;
            UpdateHighlight();
        }

        public override void Highlight(bool highlight)
        {
            _highlighted = highlight;
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            // always stay highlighted if active
            base.Highlight(_activeHand || _highlighted);
        }
    }
}
