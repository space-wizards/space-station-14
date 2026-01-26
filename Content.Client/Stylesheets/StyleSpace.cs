using System.Linq;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets
{
    [Obsolete("Please use the new sheetlet system to define styles, and remove all references to this class as it may be deleted in the future")]
    public sealed class StyleSpace : StyleBase
    {
        public static readonly Color SpaceRed = Color.FromHex("#9b2236");

        public static readonly Color ButtonColorDefault = Color.FromHex("#464966");
        public static readonly Color ButtonColorHovered = Color.FromHex("#575b7f");
        public static readonly Color ButtonColorPressed = Color.FromHex("#3e6c45");
        public static readonly Color ButtonColorDisabled = Color.FromHex("#30313c");

        public static readonly Color ButtonColorCautionDefault = Color.FromHex("#ab3232");
        public static readonly Color ButtonColorCautionHovered = Color.FromHex("#cf2f2f");
        public static readonly Color ButtonColorCautionPressed = Color.FromHex("#3e6c45");
        public static readonly Color ButtonColorCautionDisabled = Color.FromHex("#602a2a");

        public override Stylesheet Stylesheet { get; }

        public StyleSpace(IResourceCache resCache) : base(resCache)
        {
            var notoSans10 = resCache.GetFont
            (
                new []
                {
                    "/Fonts/NotoSans/NotoSans-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"
                },
                10
            );
            var notoSansBold16 = resCache.GetFont
            (
                new []
                {
                    "/Fonts/NotoSans/NotoSans-Bold.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"
                },
                16
            );

            var progressBarBackground = new StyleBoxFlat
            {
                BackgroundColor = new Color(0.25f, 0.25f, 0.25f)
            };
            progressBarBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 14.5f);

            var progressBarForeground = new StyleBoxFlat
            {
                BackgroundColor = new Color(0.25f, 0.50f, 0.25f)
            };
            progressBarForeground.SetContentMarginOverride(StyleBox.Margin.Vertical, 14.5f);

            var textureInvertedTriangle = resCache.GetTexture("/Textures/Interface/Nano/inverted_triangle.svg.png");

            var tabContainerPanel = new StyleBoxTexture();
            tabContainerPanel.SetPatchMargin(StyleBox.Margin.All, 2);

            var tabContainerBoxActive = new StyleBoxFlat {BackgroundColor = new Color(64, 64, 64)};
            tabContainerBoxActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
            var tabContainerBoxInactive = new StyleBoxFlat {BackgroundColor = new Color(32, 32, 32)};
            tabContainerBoxInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

            Stylesheet = new Stylesheet(BaseRules.Concat(new StyleRule[]
            {
                Element<Label>().Class(StyleClass.LabelHeading)
                    .Prop(Label.StylePropertyFont, notoSansBold16)
                    .Prop(Label.StylePropertyFontColor, SpaceRed),

                Element<Label>().Class(StyleClass.LabelSubText)
                    .Prop(Label.StylePropertyFont, notoSans10)
                    .Prop(Label.StylePropertyFontColor, Color.DarkGray),

                Element<PanelContainer>().Class(StyleClass.HighDivider)
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = SpaceRed, ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2
                    }),

                Element<PanelContainer>().Class(StyleClass.LowDivider)
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = Color.FromHex("#444"),
                        ContentMarginLeftOverride = 2,
                        ContentMarginBottomOverride = 2
                    }),

                // Shapes for the buttons.
                Element<PushButton>()
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButton),

                Element<PushButton>()
                    .Class(StyleClass.ButtonOpenRight)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenRight),

                Element<PushButton>()
                    .Class(StyleClass.ButtonOpenLeft)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenLeft),

                Element<PushButton>()
                    .Class(StyleClass.ButtonOpenBoth)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenBoth),

                Element<PushButton>()
                    .Class(StyleClass.ButtonSquare)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonSquare),

                // Colors for the buttons.
                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDefault),

                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorHovered),

                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorPressed),

                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDisabled),

                // Colors for the caution buttons.
                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDefault),

                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionHovered),

                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionPressed),

                Element<PushButton>()
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDisabled),

                Child()
                    .Parent(Element<PushButton>())
                    .Child(Element<Label>())
                    .Prop(Label.StylePropertyAlignMode, Label.AlignMode.Center),

                Element<PanelContainer>().Class(StyleClass.BackgroundPanel)
                    .Prop(PanelContainer.StylePropertyPanel, BaseAngleRect)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#202030")),

                Child()
                    .Parent(Element<Button>().Class(ContainerButton.StylePseudoClassDisabled))
                    .Child(Element<Label>())
                    .Prop("font-color", Color.FromHex("#E5E5E581")),

                Element<ProgressBar>()
                    .Prop(ProgressBar.StylePropertyBackground, progressBarBackground)
                    .Prop(ProgressBar.StylePropertyForeground, progressBarForeground),

                // OptionButton
                Element<OptionButton>()
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButton),

                Element<OptionButton>().Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDefault),

                Element<OptionButton>().Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorHovered),

                Element<OptionButton>().Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorPressed),

                Element<OptionButton>().Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDisabled),

                Element<TextureRect>().Class(OptionButton.StyleClassOptionTriangle)
                    .Prop(TextureRect.StylePropertyTexture, textureInvertedTriangle),

                Element<Label>().Class(OptionButton.StyleClassOptionButton)
                    .Prop(Label.StylePropertyAlignMode, Label.AlignMode.Center),

                // TabContainer
                new StyleRule(new SelectorElement(typeof(TabContainer), null, null, null),
                    new[]
                    {
                        new StyleProperty(TabContainer.StylePropertyPanelStyleBox, tabContainerPanel),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBox, tabContainerBoxActive),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBoxInactive, tabContainerBoxInactive),
                    }),

            }).ToList());
        }
    }
}
