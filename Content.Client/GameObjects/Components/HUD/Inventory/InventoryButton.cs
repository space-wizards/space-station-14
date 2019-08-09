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
        public BaseButton StorageButton { get; }

        public Action<BaseButton.ButtonEventArgs> OnPressed { get; set; }
        public Action<BaseButton.ButtonEventArgs> OnStoragePressed { get; set; }

        public InventoryButton(EquipmentSlotDefines.Slots slot, Texture texture, Texture storageTexture)
        {
            Slot = slot;

            CustomMinimumSize = (64, 64);

            AddChild(Button = new TextureButton
            {
                TextureNormal = texture,
                Scale = (2, 2),
                EnableAllKeybinds = true
            });

            Button.OnPressed += e => OnPressed?.Invoke(e);

            AddChild(SpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2)
            });

            AddChild(StorageButton = new TextureButton
            {
                TextureNormal = storageTexture,
                Scale = (0.75f, 0.75f),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.ShrinkEnd,
                Visible = false,
                EnableAllKeybinds = true
            });

            StorageButton.OnPressed += e => OnStoragePressed?.Invoke(e);
        }
    }
}
