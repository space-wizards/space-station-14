using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components;
using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components
{
    public class MagicMirrorBoundUserInterface : BoundUserInterface
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly ILocalizationManager _localization;
#pragma warning restore 649
        private MagicMirrorWindow _window;

        public MagicMirrorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new MagicMirrorWindow(this, _resourceCache, _localization);
            _window.OnClose += Close;
            _window.Open();
        }

        internal void HairSelected(string name, bool isFacialHair)
        {
            SendMessage(new SharedMagicMirrorComponent.HairSelectedMessage(name, isFacialHair));
        }

        internal void HairColorSelected(Color color, bool isFacialHair)
        {
            SendMessage(new SharedMagicMirrorComponent.HairColorSelectedMessage(color, isFacialHair));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window.Dispose();
            }
        }
    }

    public enum HairType
    {
        Hair,
        FacialHair,
    }

    public class HairPickerPanel : Control
    {
        public event Action<Color> OnHairColorPicked;
        public event Action<string> OnHairStylePicked;

        protected readonly IResourceCache ResourceCache;
        protected readonly ItemList Items;

        public HairPickerPanel(IResourceCache resourceCache, ILocalizationManager localization)
        {
            ResourceCache = resourceCache;
            var vBox = new VBoxContainer();
            AddChild(vBox);

            var colorHBox = new HBoxContainer();
            vBox.AddChild(colorHBox);

            var colorLabel = new Label
            {
                Text = localization.GetString("Color: ")
            };
            colorHBox.AddChild(colorLabel);

            var colorEdit = new LineEdit
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            colorEdit.OnTextChanged += args =>
            {
                var color = Color.TryFromHex(args.Text);
                if (color.HasValue)
                {
                    OnHairColorPicked?.Invoke(color.Value);
                }
            };
            colorHBox.AddChild(colorEdit);

            Items = new ItemList
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                CustomMinimumSize = (300, 250)
            };
            vBox.AddChild(Items);
            Items.OnItemSelected += ItemSelected;
        }

        private void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            OnHairStylePicked?.Invoke(Items[args.ItemIndex].Text);
        }

        public void Populate(HairType hairType)
        {
            ResourcePath rsiPath;
            Dictionary<string, string> map;
            switch (hairType)
            {
                case HairType.Hair:
                    rsiPath = (SharedSpriteComponent.TextureRoot / "Mob/human_hair.rsi");
                    map = HairStyles.HairStylesMap;
                    break;
                case HairType.FacialHair:
                    rsiPath = (SharedSpriteComponent.TextureRoot / "Mob/human_facial_hair.rsi");
                    map = HairStyles.FacialHairStylesMap;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hairType));
            }

            Items.Clear();
            var rsi = ResourceCache.GetResource<RSIResource>(rsiPath).RSI;
            foreach (var (styleName, styleState) in map)
            {
                Items.AddItem(styleName, rsi[styleState].Frame0);
            }
        }
    }

    public class HairPickerWindow : SS14Window
    {
        public event Action<Color> OnHairColorPicked;
        public event Action<string> OnHairStylePicked;

        protected HairPickerPanel Panel;
        protected override Vector2? CustomSize => (300, 300);

        public HairPickerWindow(IResourceCache resourceCache, ILocalizationManager localization)
        {
            Title = "Hair";
            Panel = new HairPickerPanel(resourceCache, localization);
            Panel.OnHairColorPicked += args => OnHairColorPicked?.Invoke(args);
            Panel.OnHairStylePicked += args => OnHairStylePicked?.Invoke(args);
            Contents.AddChild(Panel);
        }

        public virtual void Populate()
        {
            Panel.Populate(HairType.Hair);
        }
    }

    public class FacialHairPickerWindow : HairPickerWindow
    {
        public FacialHairPickerWindow(IResourceCache resourceCache, ILocalizationManager localization) : base(
            resourceCache, localization)
        {
            Title = "Facial hair";
        }

        public override void Populate()
        {
            Panel.Populate(HairType.FacialHair);
        }
    }

    public class MagicMirrorWindow : SS14Window
    {
        private readonly HairPickerWindow _hairPickerWindow;
        private readonly FacialHairPickerWindow _facialHairPickerWindow;

        public MagicMirrorWindow(MagicMirrorBoundUserInterface owner, IResourceCache resourceCache,
            ILocalizationManager localization)
        {
            Title = "Magic Mirror";

            _hairPickerWindow = new HairPickerWindow(resourceCache, localization);
            _hairPickerWindow.Populate();
            _hairPickerWindow.OnHairStylePicked += newStyle => owner.HairSelected(newStyle, false);
            _hairPickerWindow.OnHairColorPicked += newColor => owner.HairColorSelected(newColor, false);

            _facialHairPickerWindow = new FacialHairPickerWindow(resourceCache, localization);
            _facialHairPickerWindow.Populate();
            _facialHairPickerWindow.OnHairStylePicked += newStyle => owner.HairSelected(newStyle, true);
            _facialHairPickerWindow.OnHairColorPicked += newColor => owner.HairColorSelected(newColor, true);

            var vBox = new VBoxContainer();
            Contents.AddChild(vBox);

            var hairButton = new Button
            {
                Text = localization.GetString("Customize hair")
            };
            hairButton.OnPressed += args => _hairPickerWindow.Open();
            vBox.AddChild(hairButton);

            var facialHairButton = new Button
            {
                Text = localization.GetString("Customize facial hair")
            };
            facialHairButton.OnPressed += args => _facialHairPickerWindow.Open();
            vBox.AddChild(facialHairButton);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _hairPickerWindow.Dispose();
                _facialHairPickerWindow.Dispose();
            }
        }
    }
}
