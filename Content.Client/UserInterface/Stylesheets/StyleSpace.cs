using System.Linq;
using Content.Client.Utility;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Stylesheets
{
    public class StyleSpace : StyleBase
    {
        public static readonly Color SpaceRed = Color.FromHex("#9b2236");

        public static readonly Color ButtonColorDefault = Color.FromHex("#464966");
        public static readonly Color ButtonColorHovered = Color.FromHex("#575b7f");
        public static readonly Color ButtonColorPressed = Color.FromHex("#3e6c45");
        public static readonly Color ButtonColorDisabled = Color.FromHex("#30313c");

        public override Stylesheet Stylesheet { get; }

        public StyleSpace(IResourceCache resCache) : base(resCache)
        {
            var notoSans10 = resCache.GetFont("/Nano/NotoSans/NotoSans-Regular.ttf", 10);
            var notoSansBold16 = resCache.GetFont("/Nano/NotoSans/NotoSans-Bold.ttf", 16);

            // Button styles.
            var buttonNormal = new StyleBoxTexture(BaseButton)
            {
                Modulate = ButtonColorDefault
            };

            var buttonHover = new StyleBoxTexture(buttonNormal)
            {
                Modulate = ButtonColorHovered
            };

            var buttonPressed = new StyleBoxTexture(buttonNormal)
            {
                Modulate = ButtonColorPressed
            };

            var buttonDisabled = new StyleBoxTexture(buttonNormal)
            {
                Modulate = ButtonColorDisabled
            };


            Stylesheet = new Stylesheet(BaseRules.Concat(new StyleRule[]
            {
                // Big Label
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelHeading}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFont, notoSansBold16),
                    new StyleProperty(Label.StylePropertyFontColor, SpaceRed),
                }),

                // Small Label
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelSubText}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFont, notoSans10),
                    new StyleProperty(Label.StylePropertyFontColor, Color.DarkGray),
                }),

                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] {ClassHighDivider}, null, null), new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel,
                        new StyleBoxFlat
                        {
                            BackgroundColor = SpaceRed, ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2
                        }),
                }),

                // Regular buttons!
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { ContainerButton.StyleClassButton }, null, new[] {ContainerButton.StylePseudoClassNormal}), new[]
                {
                    new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonNormal),
                }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { ContainerButton.StyleClassButton }, null, new[] {ContainerButton.StylePseudoClassHover}), new[]
                {
                    new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonHover),
                }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { ContainerButton.StyleClassButton }, null, new[] {ContainerButton.StylePseudoClassPressed}), new[]
                {
                    new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonPressed),
                }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { ContainerButton.StyleClassButton }, null, new[] {ContainerButton.StylePseudoClassDisabled}), new[]
                {
                    new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonDisabled),
                }),

                new StyleRule(new SelectorElement(typeof(Label), new[] { Button.StyleClassButton }, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyAlignMode, Label.AlignMode.Center),
                }),

                new StyleRule(new SelectorChild(
                        new SelectorElement(typeof(Button), null, null, new[] {ContainerButton.StylePseudoClassDisabled}),
                        new SelectorElement(typeof(Label), null, null, null)),
                    new[]
                    {
                        new StyleProperty("font-color", Color.FromHex("#E5E5E581")),
                    }),
            }).ToList());
        }
    }
}
