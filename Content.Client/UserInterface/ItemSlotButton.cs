using System;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects
{
    public sealed class ItemSlotButton : MarginContainer
    {
        public TextureRect Button { get; }
        public SpriteView SpriteView { get; }
        public BaseButton StorageButton { get; }
        public TextureRect CooldownCircle { get; }

        public Action<GUIBoundKeyEventArgs> OnPressed { get; set; }
        public Action<GUIBoundKeyEventArgs> OnStoragePressed { get; set; }

        public ItemSlotButton(Texture texture, Texture storageTexture)
        {
            CustomMinimumSize = (64, 64);

            AddChild(Button = new TextureRect
            {
                Texture = texture,
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop
            });

            Button.OnKeyBindDown += OnButtonPressed;

            AddChild(SpriteView = new SpriteView
            {
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
            });

            StorageButton.OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIClick)
                {
                    OnButtonPressed(args);
                }
            };

            StorageButton.OnPressed += OnStorageButtonPressed;

            AddChild(CooldownCircle = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Stretch = TextureRect.StretchMode.KeepCentered,
                TextureScale = (2, 2),
                Visible = false,
            });
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args)
        {
            OnPressed?.Invoke(args);
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                OnStoragePressed?.Invoke(args.Event);
            }
            else
            {
                OnPressed?.Invoke(args.Event);
            }
        }
    }
}
