using System;
using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.CharacterAppearance;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.CharacterAppearance.Components.SharedMagicMirrorComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.CharacterAppearance
{
    [UsedImplicitly]
    public class MagicMirrorBoundUserInterface : BoundUserInterface
    {
        private MagicMirrorWindow? _window;

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
                    _window?.SetInitialData(initialData);
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

        internal void EyeColorSelected(Color color)
        {
            SendMessage(new EyeColorSelectedMessage((color.RByte, color.GByte, color.BByte)));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }

    public class ColorSlider : Control
    {
        private readonly Slider _slider;
        private readonly LineEdit _textBox;
        private byte _colorValue;
        private bool _ignoreEvents;

        public event Action? OnValueChanged;

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
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                MaxValue = byte.MaxValue
            };
            _textBox = new LineEdit
            {
                MinSize = (50, 0)
            };

            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
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

    public sealed class HairStylePicker : Control
    {
        [Dependency] private readonly SpriteAccessoryManager _spriteAccessoryManager = default!;

        public event Action<Color>? OnHairColorPicked;
        public event Action<string>? OnHairStylePicked;

        private readonly ItemList _items;

        private readonly Control _colorContainer;
        private readonly ColorSlider _colorSliderR;
        private readonly ColorSlider _colorSliderG;
        private readonly ColorSlider _colorSliderB;

        private Color _lastColor;
        private SpriteAccessoryCategories _categories;

        public void SetData(Color color, string styleId, SpriteAccessoryCategories categories, bool canColor)
        {
            if (_categories != categories)
            {
                _categories = categories;
                Populate();
            }

            _colorContainer.Visible = canColor;
            _lastColor = color;

            _colorSliderR.ColorValue = color.RByte;
            _colorSliderG.ColorValue = color.GByte;
            _colorSliderB.ColorValue = color.BByte;

            foreach (var item in _items)
            {
                var prototype = (SpriteAccessoryPrototype) item.Metadata!;
                item.Selected = prototype.ID == styleId;
            }

            UpdateStylePickerColor();
        }

        private void UpdateStylePickerColor()
        {
            foreach (var item in _items)
            {
                item.IconModulate = _lastColor;
            }
        }

        public HairStylePicker()
        {
            IoCManager.InjectDependencies(this);

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            AddChild(vBox);

            _colorContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            vBox.AddChild(_colorContainer);
            _colorContainer.AddChild(_colorSliderR = new ColorSlider(StyleNano.StyleClassSliderRed));
            _colorContainer.AddChild(_colorSliderG = new ColorSlider(StyleNano.StyleClassSliderGreen));
            _colorContainer.AddChild(_colorSliderB = new ColorSlider(StyleNano.StyleClassSliderBlue));

            Action colorValueChanged = ColorValueChanged;
            _colorSliderR.OnValueChanged += colorValueChanged;
            _colorSliderG.OnValueChanged += colorValueChanged;
            _colorSliderB.OnValueChanged += colorValueChanged;

            _items = new ItemList
            {
                VerticalExpand = true,
                MinSize = (300, 250)
            };
            vBox.AddChild(_items);
            _items.OnItemSelected += ItemSelected;
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

        public void Populate()
        {
            var styles = _spriteAccessoryManager
                .AccessoriesForCategory(_categories)
                .ToList();
            styles.Sort(HairStyles.SpriteAccessoryComparer);

            foreach (var style in styles)
            {
                var item = _items.AddItem(style.Name, style.Sprite.Frame0());
                item.Metadata = style;
            }
        }

        private void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            var prototype = (SpriteAccessoryPrototype?) _items[args.ItemIndex].Metadata;
            var style = prototype?.ID;

            if (style != null)
            {
                OnHairStylePicked?.Invoke(style);
            }
        }

        // ColorSlider
    }

    public class EyeColorPicker : Control
    {
        public event Action<Color>? OnEyeColorPicked;

        private readonly ColorSlider _colorSliderR;
        private readonly ColorSlider _colorSliderG;
        private readonly ColorSlider _colorSliderB;

        private Color _lastColor;

        public void SetData(Color color)
        {
            _lastColor = color;

            _colorSliderR.ColorValue = color.RByte;
            _colorSliderG.ColorValue = color.GByte;
            _colorSliderB.ColorValue = color.BByte;
        }

        public EyeColorPicker()
        {
            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            AddChild(vBox);

            vBox.AddChild(_colorSliderR = new ColorSlider(StyleNano.StyleClassSliderRed));
            vBox.AddChild(_colorSliderG = new ColorSlider(StyleNano.StyleClassSliderGreen));
            vBox.AddChild(_colorSliderB = new ColorSlider(StyleNano.StyleClassSliderBlue));

            Action colorValueChanged = ColorValueChanged;
            _colorSliderR.OnValueChanged += colorValueChanged;
            _colorSliderG.OnValueChanged += colorValueChanged;
            _colorSliderB.OnValueChanged += colorValueChanged;
        }

        private void ColorValueChanged()
        {
            var newColor = new Color(
                _colorSliderR.ColorValue,
                _colorSliderG.ColorValue,
                _colorSliderB.ColorValue
            );

            OnEyeColorPicked?.Invoke(newColor);

            _lastColor = newColor;
        }

        // ColorSlider
    }

    public class MagicMirrorWindow : SS14Window
    {
        private readonly HairStylePicker _hairStylePicker;
        private readonly HairStylePicker _facialHairStylePicker;
        private readonly EyeColorPicker _eyeColorPicker;

        public MagicMirrorWindow(MagicMirrorBoundUserInterface owner)
        {
            SetSize = MinSize = (500, 360);
            Title = Loc.GetString("magic-mirror-window-title");

            _hairStylePicker = new HairStylePicker {HorizontalExpand = true};
            _hairStylePicker.OnHairStylePicked += newStyle => owner.HairSelected(newStyle, false);
            _hairStylePicker.OnHairColorPicked += newColor => owner.HairColorSelected(newColor, false);

            _facialHairStylePicker = new HairStylePicker {HorizontalExpand = true};
            _facialHairStylePicker.OnHairStylePicked += newStyle => owner.HairSelected(newStyle, true);
            _facialHairStylePicker.OnHairColorPicked += newColor => owner.HairColorSelected(newColor, true);

            _eyeColorPicker = new EyeColorPicker { HorizontalExpand = true };
            _eyeColorPicker.OnEyeColorPicked += newColor => owner.EyeColorSelected(newColor);

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 8,
                Children = {_hairStylePicker, _facialHairStylePicker, _eyeColorPicker}
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _hairStylePicker.Dispose();
                _facialHairStylePicker.Dispose();
                _eyeColorPicker.Dispose();
            }
        }

        public void SetInitialData(MagicMirrorInitialDataMessage initialData)
        {
            _facialHairStylePicker.SetData(initialData.FacialHairColor, initialData.FacialHairId, initialData.CategoriesFacialHair, initialData.CanColorFacialHair);
            _hairStylePicker.SetData(initialData.HairColor, initialData.HairId, initialData.CategoriesHair, initialData.CanColorHair);
            _eyeColorPicker.SetData(initialData.EyeColor);
        }
    }
}
