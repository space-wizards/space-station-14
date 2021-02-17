using System;
using System.Linq;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Preferences.Appearance;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.SharedMagicMirrorComponent;
using static Content.Client.StaticIoC;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class MagicMirrorBoundUserInterface : BoundUserInterface
    {
        private MagicMirrorWindow _window;

        public MagicMirrorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new MagicMirrorWindow(this);
            _window.OnClose += Close;
            _window.Open();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case MagicMirrorInitialDataMessage initialData:
                    _window.SetInitialData(initialData);
                    break;
            }
        }

        internal void HairSelected(string name, bool isFacialHair)
        {
            SendMessage(new HairSelectedMessage(name, isFacialHair));
        }

        internal void HairColorSelected(Color color, bool isFacialHair)
        {
            SendMessage(new HairColorSelectedMessage((color.RByte, color.GByte, color.BByte),
                isFacialHair));
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

    public class FacialHairStylePicker : HairStylePicker
    {
        public override void Populate()
        {
            var humanFacialHairRSIPath = SharedSpriteComponent.TextureRoot / "Mobs/Customization/human_facial_hair.rsi";
            var humanFacialHairRSI = ResC.GetResource<RSIResource>(humanFacialHairRSIPath).RSI;

            var styles = HairStyles.FacialHairStylesMap.ToList();
            styles.Sort(HairStyles.FacialHairStyleComparer);

            foreach (var (styleName, styleState) in HairStyles.FacialHairStylesMap)
            {
                Items.AddItem(styleName, humanFacialHairRSI[styleState].Frame0);
            }
        }
    }

    public class HairStylePicker : Control
    {
        public event Action<Color> OnHairColorPicked;
        public event Action<string> OnHairStylePicked;

        protected readonly ItemList Items;

        private readonly ColorSlider _colorSliderR;
        private readonly ColorSlider _colorSliderG;
        private readonly ColorSlider _colorSliderB;

        private Color _lastColor;

        public void SetData(Color color, string styleName)
        {
            _lastColor = color;

            _colorSliderR.ColorValue = color.RByte;
            _colorSliderG.ColorValue = color.GByte;
            _colorSliderB.ColorValue = color.BByte;

            foreach (var item in Items)
            {
                item.Selected = item.Text == styleName;
            }

            UpdateStylePickerColor();
        }

        private void UpdateStylePickerColor()
        {
            foreach (var item in Items)
            {
                item.IconModulate = _lastColor;
            }
        }

        public HairStylePicker()
        {
            var vBox = new VBoxContainer();
            AddChild(vBox);

            vBox.AddChild(_colorSliderR = new ColorSlider(StyleNano.StyleClassSliderRed));
            vBox.AddChild(_colorSliderG = new ColorSlider(StyleNano.StyleClassSliderGreen));
            vBox.AddChild(_colorSliderB = new ColorSlider(StyleNano.StyleClassSliderBlue));

            Action colorValueChanged = ColorValueChanged;
            _colorSliderR.OnValueChanged += colorValueChanged;
            _colorSliderG.OnValueChanged += colorValueChanged;
            _colorSliderB.OnValueChanged += colorValueChanged;

            Items = new ItemList
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                CustomMinimumSize = (300, 250)
            };
            vBox.AddChild(Items);
            Items.OnItemSelected += ItemSelected;
        }

        private void ColorValueChanged()
        {
            var newColor = new Color(
                _colorSliderR.ColorValue,
                _colorSliderG.ColorValue,
                _colorSliderB.ColorValue
            );

            OnHairColorPicked?.Invoke(newColor);
            _lastColor = newColor;
            UpdateStylePickerColor();
        }

        public virtual void Populate()
        {
            var humanHairRSIPath = SharedSpriteComponent.TextureRoot / "Mobs/Customization/human_hair.rsi";
            var humanHairRSI = ResC.GetResource<RSIResource>(humanHairRSIPath).RSI;

            var styles = HairStyles.HairStylesMap.ToList();
            styles.Sort(HairStyles.HairStyleComparer);

            foreach (var (styleName, styleState) in styles)
            {
                Items.AddItem(styleName, humanHairRSI[styleState].Frame0);
            }
        }

        private void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            OnHairStylePicked?.Invoke(Items[args.ItemIndex].Text);
        }

        private sealed class ColorSlider : Control
        {
            private readonly Slider _slider;
            private readonly LineEdit _textBox;
            private byte _colorValue;
            private bool _ignoreEvents;

            public event Action OnValueChanged;

            public byte ColorValue
            {
                get => _colorValue;
                set
                {
                    _ignoreEvents = true;
                    _colorValue = value;
                    _slider.Value = value;
                    _textBox.Text = value.ToString();
                    _ignoreEvents = false;
                }
            }

            public ColorSlider(string styleClass)
            {
                _slider = new Slider
                {
                    StyleClasses = {styleClass},
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    MaxValue = byte.MaxValue
                };
                _textBox = new LineEdit
                {
                    CustomMinimumSize = (50, 0)
                };

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        _slider,
                        _textBox
                    }
                });

                _slider.OnValueChanged += _ =>
                {
                    if (_ignoreEvents)
                    {
                        return;
                    }

                    _colorValue = (byte) _slider.Value;
                    _textBox.Text = _colorValue.ToString();

                    OnValueChanged?.Invoke();
                };

                _textBox.OnTextChanged += ev =>
                {
                    if (_ignoreEvents)
                    {
                        return;
                    }

                    if (int.TryParse(ev.Text, out var result))
                    {
                        result = MathHelper.Clamp(result, 0, byte.MaxValue);

                        _ignoreEvents = true;
                        _colorValue = (byte) result;
                        _slider.Value = result;
                        _ignoreEvents = false;

                        OnValueChanged?.Invoke();
                    }
                };
            }
        }
    }

    public class MagicMirrorWindow : SS14Window
    {
        private readonly HairStylePicker _hairStylePicker;
        private readonly FacialHairStylePicker _facialHairStylePicker;

        protected override Vector2? CustomSize => (500, 360);

        public MagicMirrorWindow(MagicMirrorBoundUserInterface owner)
        {
            Title = Loc.GetString("Magic Mirror");

            _hairStylePicker = new HairStylePicker {SizeFlagsHorizontal = SizeFlags.FillExpand};
            _hairStylePicker.Populate();
            _hairStylePicker.OnHairStylePicked += newStyle => owner.HairSelected(newStyle, false);
            _hairStylePicker.OnHairColorPicked += newColor => owner.HairColorSelected(newColor, false);

            _facialHairStylePicker = new FacialHairStylePicker {SizeFlagsHorizontal = SizeFlags.FillExpand};
            _facialHairStylePicker.Populate();
            _facialHairStylePicker.OnHairStylePicked += newStyle => owner.HairSelected(newStyle, true);
            _facialHairStylePicker.OnHairColorPicked += newColor => owner.HairColorSelected(newColor, true);

            Contents.AddChild(new HBoxContainer
            {
                SeparationOverride = 8,
                Children = {_hairStylePicker, _facialHairStylePicker}
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _hairStylePicker.Dispose();
                _facialHairStylePicker.Dispose();
            }
        }

        public void SetInitialData(MagicMirrorInitialDataMessage initialData)
        {
            _facialHairStylePicker.SetData(initialData.FacialHairColor, initialData.FacialHairName);
            _hairStylePicker.SetData(initialData.HairColor, initialData.HairName);
        }
    }
}
