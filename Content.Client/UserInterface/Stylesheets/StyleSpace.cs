using System.Linq;
using Content.Client.Utility;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

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
            var notoSans10 = resCache.GetFont("/Textures/Interface/Nano/NotoSans/NotoSans-Regular.ttf", 10);
            var notoSansBold16 = resCache.GetFont("/Textures/Interface/Nano/NotoSans/NotoSans-Bold.ttf", 16);

            static (StyleBox, StyleBox, StyleBox, StyleBox) ButtonPermutations(StyleBoxTexture @base)
            {
                var normal = new StyleBoxTexture(@base) {Modulate = ButtonColorDefault};
                var hover = new StyleBoxTexture(@base) {Modulate = ButtonColorHovered};
                var pressed = new StyleBoxTexture(@base) {Modulate = ButtonColorPressed};
                var disabled = new StyleBoxTexture(@base) {Modulate = ButtonColorDisabled};

                return (normal, hover, pressed, disabled);
            }

            // Button styles.
            var (buttonNormal, buttonHover, buttonPressed, buttonDisabled)
                = ButtonPermutations(BaseButton);

            var (buttonRNormal, buttonRHover, buttonRPressed, buttonRDisabled)
                = ButtonPermutations(BaseButtonOpenRight);

            var (buttonLNormal, buttonLHover, buttonLPressed, buttonLDisabled)
                = ButtonPermutations(BaseButtonOpenLeft);

            var (buttonBNormal, buttonBHover, buttonBPressed, buttonBDisabled)
                = ButtonPermutations(BaseButtonOpenBoth);

            Stylesheet = new Stylesheet(BaseRules.Concat(new StyleRule[]
            {
                Element<Label>().Class(StyleClassLabelHeading)
                    .Prop(Label.StylePropertyFont, notoSansBold16)
                    .Prop(Label.StylePropertyFontColor, SpaceRed),

                Element<Label>().Class(StyleClassLabelSubText)
                    .Prop(Label.StylePropertyFont, notoSans10)
                    .Prop(Label.StylePropertyFontColor, Color.DarkGray),

                Element<PanelContainer>().Class(ClassHighDivider)
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = SpaceRed, ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2
                    }),

                // Normal buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonNormal),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonHover),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonDisabled),

                // Right open buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenRight)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonRNormal),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenRight)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonRHover),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenRight)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonRPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenRight)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonRDisabled),

                // Left open buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenLeft)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonLNormal),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenLeft)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonLHover),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenLeft)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonLPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenLeft)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonLDisabled),

                // "Both" open buttons
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenBoth)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBNormal),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenBoth)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBHover),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenBoth)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonOpenBoth)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBDisabled),


                Element<Label>().Class(ContainerButton.StyleClassButton)
                    .Prop(Label.StylePropertyAlignMode, Label.AlignMode.Center),

                Child()
                    .Parent(Element<Button>().Class(ContainerButton.StylePseudoClassDisabled))
                    .Child(Element<Label>())
                    .Prop("font-color", Color.FromHex("#E5E5E581")),

            }).ToList());
        }
    }
}
