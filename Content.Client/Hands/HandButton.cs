using Content.Client.HUD;
using Content.Client.Items.UI;
using Content.Shared.Hands.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Hands
{
    public class HandButton : ItemSlotButton
    {
        private bool _activeHand;
        private bool _highlighted;

        public HandButton(int size, string textureName, string storageTextureName, IGameHud gameHud, Texture blockedTexture, HandLocation location) : base(size, textureName, storageTextureName, gameHud)
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
