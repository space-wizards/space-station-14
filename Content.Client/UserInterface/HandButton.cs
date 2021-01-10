using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

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
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });
        }

        public HandLocation Location { get; }
        public TextureRect Blocked { get; }

        /// <summary>
        /// Lights this hand up to appear active
        /// </summary>
        /// <param name="highlight">whether to appear active</param>
        public void SetActiveHighlight(bool highlight)
        {
            if (highlight)
            {
                Button.ModulateSelfOverride = Color.White;
            }
            else
            {
                Button.ModulateSelfOverride = null;
            }
        }
    }
}
