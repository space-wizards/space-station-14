#nullable enable
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.GameObjects.Components.Mobs
{
    public class AlertControl : BaseButton
    {
        public readonly AlertSlot Effect;

        public AlertControl(AlertSlot effect, Texture? texture)
        {
            Effect = effect;

            var item = new TextureRect
            {
                TextureScale = (2, 2),
                Texture = texture
            };

            Children.Add(item);
        }
    }
}
