using Content.Client.Utility;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Stylesheets
{
    public abstract class StyleBase
    {
        public const string ClassHighDivider = "HighDivider";
        public const string StyleClassLabelHeading = "LabelHeading";
        public const string StyleClassLabelSubText = "LabelSubText";

        public abstract Stylesheet Stylesheet { get; }

        protected StyleRule[] BaseRules { get; }

        protected StyleBoxTexture BaseButton { get; }

        protected StyleBase(IResourceCache resCache)
        {
            var notoSans12 = resCache.GetFont("/Textures/Interface/Nano/NotoSans/NotoSans-Regular.ttf", 12);

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

            BaseRules = new[]
            {
                // Default font.
                new StyleRule(
                    new SelectorElement(null, null, null, null),
                    new[]
                    {
                        new StyleProperty("font", notoSans12),
                    }),
            };
        }
    }
}
