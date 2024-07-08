using Content.Client.Stylesheets.Redux.Fonts;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class LabelSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<Label>()
                .Class(StyleClass.LabelHeading)
                .Font(sheet.BaseFont.GetFont(16, FontStack.FontKind.Bold))
                .FontColor(sheet.HighlightPalette[0]),
            E<Label>()
                .Class(StyleClass.LabelHeadingBigger)
                .Font(sheet.BaseFont.GetFont(20, FontStack.FontKind.Bold))
                .FontColor(sheet.HighlightPalette[0]),
            E<Label>()
                .Class(StyleClass.LabelSubText)
                .Font(sheet.BaseFont.GetFont(10))
                .FontColor(Color.DarkGray),
            E<Label>()
                .Class(StyleClass.LabelKeyText)
                .Font(sheet.BaseFont.GetFont(12, FontStack.FontKind.Bold))
                .FontColor(sheet.HighlightPalette[0]),
            E<Label>()
                .Class(StyleClass.LabelWeak)
                .FontColor(Color.DarkGray), // TODO: you know the drill by now
            E<Label>()
                .Class(StyleClass.Positive)
                .FontColor(sheet.PositivePalette[0]),
            E<Label>()
                .Class(StyleClass.Negative)
                .FontColor(sheet.NegativePalette[0]),
            E<Label>()
                .Class(StyleClass.Highlight)
                .FontColor(sheet.HighlightPalette[0]),
        ];
    }
}
