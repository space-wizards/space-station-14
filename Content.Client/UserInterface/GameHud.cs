using System;
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public interface IGameHud
    {
        Control RootControl { get; }

        bool EscapeButtonDown { get; set; }
        Action<bool> EscapeButtonToggled { get; set; }
        void Initialize();
    }

    /// <summary>
    ///     Responsible for laying out the default game HUD.
    /// </summary>
    internal sealed class GameHud : IGameHud
    {
        private TopButton _buttonEscapeMenu;

#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        public void Initialize()
        {
            RootControl = new Control();

            RootControl.SetAnchorPreset(Control.LayoutPreset.Wide);

            var escapeTexture = _resourceCache.GetTexture("/Textures/UserInterface/hamburger.svg.96dpi.png");

            _buttonEscapeMenu = new TopButton(escapeTexture, "esc")
            {
                ToolTip = _localizationManager.GetString("Open escape menu.")
            };

            _buttonEscapeMenu.OnToggled += args => EscapeButtonToggled?.Invoke(args.Pressed);

            RootControl.AddChild(_buttonEscapeMenu);
            _buttonEscapeMenu.SetAnchorAndMarginPreset(Control.LayoutPreset.TopLeft, margin: 10);
        }

        public Control RootControl { get; private set; }

        public bool EscapeButtonDown
        {
            get => _buttonEscapeMenu.Pressed;
            set => _buttonEscapeMenu.Pressed = value;
        }

        public Action<bool> EscapeButtonToggled { get; set; }


        public sealed class TopButton : BaseButton
        {
            private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
            private static readonly Color ColorHovered = Color.FromHex("#9699bb");
            private static readonly Color ColorPressed = Color.FromHex("#00b061");

            private readonly VBoxContainer _container;
            private readonly TextureRect _textureRect;
            private readonly Label _label;

            public TopButton(Texture texture, string keyName)
            {
                ToggleMode = true;

                _container = new VBoxContainer {MouseFilter = MouseFilterMode.Ignore};
                AddChild(_container);
                _container.AddChild(_textureRect = new TextureRect
                {
                    Texture = texture,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterMode.Ignore,
                    ModulateSelfOverride = ColorNormal
                });

                _container.AddChild(_label = new Label
                {
                    Text = keyName,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    MouseFilter = MouseFilterMode.Ignore,
                    ModulateSelfOverride = ColorNormal
                });

                _container.SetAnchorAndMarginPreset(LayoutPreset.Wide);

                DrawModeChanged();
            }

            protected override Vector2 CalculateMinimumSize()
            {
                var styleSize = ActualStyleBox?.MinimumSize ?? Vector2.Zero;
                return (0, 4) + styleSize + _container?.CombinedMinimumSize ?? Vector2.Zero;
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                ActualStyleBox?.Draw(handle, PixelSizeBox);
            }

            private StyleBox ActualStyleBox
            {
                get
                {
                    TryGetStyleProperty(Button.StylePropertyStyleBox, out StyleBox ret);
                    return ret;
                }
            }

            protected override void DrawModeChanged()
            {
                switch (DrawMode)
                {
                    case DrawModeEnum.Normal:
                        StylePseudoClass = Button.StylePseudoClassNormal;
                        _textureRect.ModulateSelfOverride = ColorNormal;
                        _label.ModulateSelfOverride = ColorNormal;
                        break;

                    case DrawModeEnum.Pressed:
                        StylePseudoClass = Button.StylePseudoClassPressed;
                        _textureRect.ModulateSelfOverride = ColorPressed;
                        _label.ModulateSelfOverride = ColorPressed;
                        break;

                    case DrawModeEnum.Hover:
                        StylePseudoClass = Button.StylePseudoClassHover;
                        _textureRect.ModulateSelfOverride = ColorHovered;
                        _label.ModulateSelfOverride = ColorHovered;
                        break;

                    case DrawModeEnum.Disabled:
                        break;
                }
            }

            protected override void StylePropertiesChanged()
            {
                base.StylePropertiesChanged();

                if (_container == null)
                {
                    return;
                }

                var box = ActualStyleBox ?? new StyleBoxEmpty();

                _container.MarginLeft = box.GetContentMargin(StyleBox.Margin.Left);
                _container.MarginRight = -box.GetContentMargin(StyleBox.Margin.Right);
                _container.MarginTop = box.GetContentMargin(StyleBox.Margin.Top) + 4;
                _container.MarginBottom = -box.GetContentMargin(StyleBox.Margin.Bottom);
            }
        }
    }
}
