using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects
{
    public sealed class ItemSlotButton : MarginContainer
    {
        public BaseButton Button { get; }
        public SpriteView SpriteView { get; }
        public BaseButton StorageButton { get; }
        public TextureRect CooldownCircle { get; }

        public Action<BaseButton.ButtonEventArgs> OnPressed { get; set; }
        public Action<BaseButton.ButtonEventArgs> OnStoragePressed { get; set; }

        public ItemSlotButton(Texture texture, Texture storageTexture)
        {
            CustomMinimumSize = (64, 64);

            AddChild(Button = new TextureButton
            {
                TextureNormal = texture,
                Scale = (2, 2),
                EnableAllKeybinds = true
            });

            Button.OnPressed += OnButtonPressed;

            AddChild(SpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2),
                OverrideDirection = Direction.South
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

            AddChild(CooldownCircle = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterMode.Ignore,
                Stretch = TextureRect.StretchMode.KeepCentered,
                TextureScale = (2, 2),
                Visible = false,
            });
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
