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

        public static readonly Color ButtonColorCautionDefault = Color.FromHex("#ab3232");
        public static readonly Color ButtonColorCautionHovered = Color.FromHex("#cf2f2f");
        public static readonly Color ButtonColorCautionPressed = Color.FromHex("#3e6c45");
        public static readonly Color ButtonColorCautionDisabled = Color.FromHex("#602a2a");

        public override Stylesheet Stylesheet { get; }

        public StyleSpace(IResourceCache resCache) : base(resCache)
        {
            var notoSans10 = resCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 10);
            var notoSansBold16 = resCache.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 16);

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
                // Shapes for the buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButton),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenRight)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenRight),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenLeft)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenLeft),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenBoth)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenBoth),

                // Colors for the buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDefault),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorHovered),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDisabled),

                // Colors for the caution buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDefault),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionHovered),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDisabled),


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
