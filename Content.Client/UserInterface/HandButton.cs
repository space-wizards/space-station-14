using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface
{
    public class HandButton : ItemSlotButton
    {
        public HandButton(Texture texture, Texture storageTexture, Texture blockedTexture, HandLocation location) : base(texture, storageTexture)
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
    }
}
