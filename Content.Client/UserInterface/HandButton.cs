using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface
{
    public class HandButton : ItemSlotButton
    {
        public HandButton(Texture texture, Texture storageTexture, HandLocation location) : base(texture, storageTexture)
        {
            Location = location;
        }

        public HandLocation Location { get; }
    }
}
