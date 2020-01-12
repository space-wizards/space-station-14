using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.HUD
{
    public class ItemSlotButton : MarginContainer
    {
        public IEntity Item { get; set; }

        public BaseButton Button { get; }
        public SpriteView SpriteView { get; }
        public BaseButton StorageButton { get; }

        public Action<BaseButton.ButtonEventArgs> OnPressed { get; set; }
        public Action<BaseButton.ButtonEventArgs> OnStoragePressed { get; set; }

        public ItemSlotButton(Texture texture, Texture storageTexture)
        {
            Item = null;
            CustomMinimumSize = (64, 64);

            AddChild(Button = new TextureButton
            {
                TextureNormal = texture,
                Scale = (2, 2),
                EnableAllKeybinds = true
            });

            Button.OnButtonDown += OnButtonPressed;

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

            StorageButton.OnPressed += OnStorageButtonPressed;
        }

        private void OnButtonPressed(BaseButton.ButtonEventArgs args)
        {
            OnPressed?.Invoke(args);
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.Use)
            {
                OnStoragePressed?.Invoke(args);
            }
            else
            {
                OnPressed?.Invoke(args);
            }
        }

    }
}
