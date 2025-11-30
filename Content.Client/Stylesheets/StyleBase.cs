using System.Numerics;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.Stylesheets
{
    [Obsolete("Please use the new sheetlet system to define styles, and remove all references to this class as it may be deleted in the future")]
    public abstract class StyleBase
    {
        public abstract Stylesheet Stylesheet { get; }

        protected StyleRule[] BaseRules { get; }

        protected StyleBoxTexture BaseButton { get; }
        protected StyleBoxTexture BaseButtonOpenRight { get; }
        protected StyleBoxTexture BaseButtonOpenLeft { get; }
        protected StyleBoxTexture BaseButtonOpenBoth { get; }
        protected StyleBoxTexture BaseButtonSquare { get; }

        protected StyleBoxTexture BaseAngleRect { get; }
        protected StyleBoxTexture AngleBorderRect { get; }

        protected StyleBase(IResourceCache resCache)
        {
            var notoSans12 = resCache.GetFont
            (
                new []
                {
                    "/Fonts/NotoSans/NotoSans-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"
                },
                12
            );
            var notoSans12Italic = resCache.GetFont
            (
                new []
                {
                    "/Fonts/NotoSans/NotoSans-Italic.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"
                },
                12
            );
            var textureCloseButton = resCache.GetTexture("/Textures/Interface/Nano/cross.svg.png");

            // Button styles.
            var buttonTex = resCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            BaseButton = new StyleBoxTexture
            {
                Texture = buttonTex,
            };
            BaseButton.SetPatchMargin(StyleBox.Margin.All, 10);
            BaseButton.SetPadding(StyleBox.Margin.All, 1);
            BaseButton.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            BaseButton.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);

            BaseButtonOpenRight = new StyleBoxTexture(BaseButton)
            {
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
            };
            BaseButtonOpenRight.SetPatchMargin(StyleBox.Margin.Right, 0);
            BaseButtonOpenRight.SetContentMarginOverride(StyleBox.Margin.Right, 8);
            BaseButtonOpenRight.SetPadding(StyleBox.Margin.Right, 2);

            BaseButtonOpenLeft = new StyleBoxTexture(BaseButton)
            {
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
            };
            BaseButtonOpenLeft.SetPatchMargin(StyleBox.Margin.Left, 0);
            BaseButtonOpenLeft.SetContentMarginOverride(StyleBox.Margin.Left, 8);
            BaseButtonOpenLeft.SetPadding(StyleBox.Margin.Left, 1);

            BaseButtonOpenBoth = new StyleBoxTexture(BaseButton)
            {
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
            };
            BaseButtonOpenBoth.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
            BaseButtonOpenBoth.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
            BaseButtonOpenBoth.SetPadding(StyleBox.Margin.Right, 2);
            BaseButtonOpenBoth.SetPadding(StyleBox.Margin.Left, 1);

            BaseButtonSquare = new StyleBoxTexture(BaseButton)
            {
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
            };
            BaseButtonSquare.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
            BaseButtonSquare.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
            BaseButtonSquare.SetPadding(StyleBox.Margin.Right, 2);
            BaseButtonSquare.SetPadding(StyleBox.Margin.Left, 1);

            BaseAngleRect = new StyleBoxTexture
            {
                Texture = buttonTex,
            };
            BaseAngleRect.SetPatchMargin(StyleBox.Margin.All, 10);

            AngleBorderRect = new StyleBoxTexture
            {
                Texture = resCache.GetTexture("/Textures/Interface/Nano/geometric_panel_border.svg.96dpi.png"),
            };
            AngleBorderRect.SetPatchMargin(StyleBox.Margin.All, 10);


            BaseRules = new[]
            {
                // Default font.
                new StyleRule(
                    new SelectorElement(null, null, null, null),
                    new[]
                    {
                        new StyleProperty("font", notoSans12),
                    }),

                // Default font.
                new StyleRule(
                    new SelectorElement(null, new[] {StyleClass.Italic}, null, null),
                    new[]
                    {
                        new StyleProperty("font", notoSans12Italic),
                    }),

                // Window close button base texture.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {DefaultWindow.StyleClassWindowCloseButton}, null,
                        null),
                    new[]
                    {
                        new StyleProperty(TextureButton.StylePropertyTexture, textureCloseButton),
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#4B596A")),
                    }),
                // Window close button hover.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {DefaultWindow.StyleClassWindowCloseButton}, null,
                        new[] {TextureButton.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#7F3636")),
                    }),
                // Window close button pressed.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {DefaultWindow.StyleClassWindowCloseButton}, null,
                        new[] {TextureButton.StylePseudoClassPressed}),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#753131")),
                    }),
            };
        }
    }
}
