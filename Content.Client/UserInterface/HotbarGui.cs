using System;
using System.Collections.Generic;
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    public class HotbarGui : PanelContainer
    {
        private List<HotbarButton> _slots = new List<HotbarButton>();

        private VBoxContainer _hotbarContainer;
        private VBoxContainer _slotContainer;

        public TextureButton LockButton;
        public TextureButton SettingsButton;
        public TextureButton PreviousHotbarButton;
        public Label LoadoutNumber;
        public TextureButton NextHotbarButton;

        public event Action<BaseButton.ButtonToggledEventArgs, int> OnSlotToggled;

        public HotbarGui()
        {
            SizeFlagsHorizontal = SizeFlags.FillExpand;
            SizeFlagsVertical = SizeFlags.FillExpand;

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            _hotbarContainer = new VBoxContainer
            {
                SeparationOverride = 3
            };
            AddChild(_hotbarContainer);

            var settingsContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _hotbarContainer.AddChild(settingsContainer);

            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });
            LockButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Nano/lock.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            settingsContainer.AddChild(LockButton);
            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            SettingsButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Nano/gear.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            settingsContainer.AddChild(SettingsButton);
            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });

            _slotContainer = new VBoxContainer();
            _hotbarContainer.AddChild(_slotContainer);

            var loadoutContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _hotbarContainer.AddChild(loadoutContainer);

            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });
            PreviousHotbarButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Nano/left_arrow.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            loadoutContainer.AddChild(PreviousHotbarButton);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            LoadoutNumber = new Label
            {
                Text = "1",
                SizeFlagsStretchRatio = 1
            };
            loadoutContainer.AddChild(LoadoutNumber);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            NextHotbarButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Nano/right_arrow.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            loadoutContainer.AddChild(NextHotbarButton);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });

            HotbarButton CreateSlot(int index)
            {
                var button = new HotbarButton(null, index) { };
                button.OnToggled += args =>
                {
                    OnSlotToggled?.Invoke(args, index);
                };
                _slots.Add(button);
                return button;
            }

            var zero = CreateSlot(0);
            _slotContainer.AddChild(CreateSlot(1));
            _slotContainer.AddChild(CreateSlot(2));
            _slotContainer.AddChild(CreateSlot(3));
            _slotContainer.AddChild(CreateSlot(4));
            _slotContainer.AddChild(CreateSlot(5));
            _slotContainer.AddChild(CreateSlot(6));
            _slotContainer.AddChild(CreateSlot(7));
            _slotContainer.AddChild(CreateSlot(8));
            _slotContainer.AddChild(CreateSlot(9));
            _slotContainer.AddChild(zero);
        }

        public void SetSlot(int index, Texture texture, bool pressed)
        {
            if (index < 0 || index >= _slots.Count)
            {
                return;
            }
            _slots[index].Texture.Texture = texture;
            SetSlotPressed(index, pressed);
        }

        public void SetSlotPressed(int index, bool pressed)
        {
            if (index < 0 || index >= _slots.Count)
            {
                return;
            }
            _slots[index].Pressed = pressed;
        }

        public class HotbarButton : ContainerButton
        {
            public const string StyleClassButtonRect = "buttonRect";

            public TextureRect Texture;

            public int Index;

            public HotbarButton(Texture background, int index)
            {
                AddStyleClass(StyleClassButtonRect);
                CustomMinimumSize = (64, 64);
                ToggleMode = true;

                Index = index;

                AddChild(new Label
                {
                    Text = index.ToString(),
                    SizeFlagsVertical = SizeFlags.None
                });

                Texture = new TextureRect
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    Stretch = TextureRect.StretchMode.Scale
                };
                AddChild(Texture);
            }
        }
    }
}
