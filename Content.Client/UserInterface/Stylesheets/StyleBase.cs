using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Stylesheets
{
    public abstract class StyleBase
    {
        public const string ClassHighDivider = "HighDivider";
        public const string StyleClassLabelHeading = "LabelHeading";
        public const string StyleClassLabelSubText = "LabelSubText";
        public const string StyleClassItalic = "Italic";

        public const string ButtonOpenRight = "OpenRight";
        public const string ButtonOpenLeft = "OpenLeft";
        public const string ButtonOpenBoth = "OpenBoth";
        public const string ButtonSquare = "ButtonSquare";

        public const string ButtonCaution = "Caution";

        public abstract Stylesheet Stylesheet { get; }

        protected StyleRule[] BaseRules { get; }

        protected StyleBoxTexture BaseButton { get; }
        protected StyleBoxTexture BaseButtonOpenRight { get; }
        protected StyleBoxTexture BaseButtonOpenLeft { get; }
        protected StyleBoxTexture BaseButtonOpenBoth { get; }
        protected StyleBoxTexture BaseButtonSquare { get; }

        protected StyleBase(IResourceCache resCache)
        {
            var notoSans12 = resCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
            var notoSans12Italic = resCache.GetFont("/Fonts/NotoSans/NotoSans-Italic.ttf", 12);

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
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions((0, 0), (14, 24))),
            };
            BaseButtonOpenRight.SetPatchMargin(StyleBox.Margin.Right, 0);
            BaseButtonOpenRight.SetContentMarginOverride(StyleBox.Margin.Right, 8);
            BaseButtonOpenRight.SetPadding(StyleBox.Margin.Right, 2);

            BaseButtonOpenLeft = new StyleBoxTexture(BaseButton)
            {
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions((10, 0), (14, 24))),
            };
            BaseButtonOpenLeft.SetPatchMargin(StyleBox.Margin.Left, 0);
            BaseButtonOpenLeft.SetContentMarginOverride(StyleBox.Margin.Left, 8);
            BaseButtonOpenLeft.SetPadding(StyleBox.Margin.Left, 1);

            BaseButtonOpenBoth = new StyleBoxTexture(BaseButton)
            {
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions((10, 0), (3, 24))),
            };
            BaseButtonOpenBoth.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
            BaseButtonOpenBoth.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
            BaseButtonOpenBoth.SetPadding(StyleBox.Margin.Right, 2);
            BaseButtonOpenBoth.SetPadding(StyleBox.Margin.Left, 1);

            BaseButtonSquare = new StyleBoxTexture(BaseButton)
            {
                Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions((10, 0), (3, 24))),
            };
            BaseButtonSquare.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
            BaseButtonSquare.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
            BaseButtonSquare.SetPadding(StyleBox.Margin.Right, 2);
            BaseButtonSquare.SetPadding(StyleBox.Margin.Left, 1);

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
                    new SelectorElement(null, new[] {StyleClassItalic}, null, null),
                    new[]
                    {
                        new StyleProperty("font", notoSans12Italic),
                    }),
            };
        }
    }
}
