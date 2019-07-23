using System;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects
{
    public sealed class InventoryButton : MarginContainer
    {
        public EquipmentSlotDefines.Slots Slot { get; }
        public EntityUid EntityUid { get; set; }

        public BaseButton Button { get; }
        public SpriteView SpriteView { get; }

        public Action<BaseButton.ButtonEventArgs> OnPressed { get; set; }

        public InventoryButton(EquipmentSlotDefines.Slots slot, Texture texture)
        {
            Slot = slot;

            CustomMinimumSize = (64, 64);

            AddChild(Button = new TextureButton
            {
                TextureNormal = texture,
                Scale = (2, 2),
            });

            Button.OnPressed += e => OnPressed?.Invoke(e);

            AddChild(SpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2)
            });
        }
    }
}
