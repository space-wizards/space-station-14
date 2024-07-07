using Content.Client.Stylesheets.Redux.Fonts;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class FancyWindowSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var boxFont = new SingleFont(ResCache, "/Fonts/Boxfont-round/Boxfont Round.ttf");
        return
        [
            /*
             * Title.
             */
            E<Label>()
                .Class("FancyWindowTitle")
                .Prop("font", boxFont.GetFont(13, FontStack.FontKind.Bold))
                .Prop("font-color", sheet.HighlightPalette[0]),

            /*
             * Help button.
             */
            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture("help.png"))
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette[1]),

            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette[0]),

            E<TextureButton>()
                .Class(FancyWindow.StyleClassWindowHelpButton)
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette[2]),

            /*
             * Footer
             */
            E<Label>()
                .Class("WindowFooterText")
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(8))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#757575")),
        ];
    }
}
