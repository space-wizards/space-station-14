using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class LabelSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<Label>()
                .Class(StyleClass.LabelHeading)
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold))
                .FontColor(sheet.HighlightPalette.Text),
            E<Label>()
                .Class(StyleClass.LabelHeadingBigger)
                .Font(sheet.BaseFont.GetFont(20, FontKind.Bold))
                .FontColor(sheet.HighlightPalette.Text),
            E<Label>()
                .Class(StyleClass.LabelSubText)
                .Font(sheet.BaseFont.GetFont(10))
                .FontColor(Color.DarkGray),
            E<Label>()
                .Class(StyleClass.LabelKeyText)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold))
                .FontColor(sheet.HighlightPalette.Text),
            E<Label>()
                .Class(StyleClass.LabelWeak)
                .FontColor(Color.DarkGray), // TODO: you know the drill by now

            E<Label>()
                .Class(StyleClass.Positive)
                .FontColor(sheet.PositivePalette.Text),
            E<Label>()
                .Class(StyleClass.Negative)
                .FontColor(sheet.NegativePalette.Text),
            E<Label>()
                .Class(StyleClass.Highlight)
                .FontColor(sheet.HighlightPalette.Text),

            E<Label>()
                .Class(StyleClass.StatusGood)
                .FontColor(Palettes.Status.Good),
            E<Label>()
                .Class(StyleClass.StatusOkay)
                .FontColor(Palettes.Status.Okay),
            E<Label>()
                .Class(StyleClass.StatusWarning)
                .FontColor(Palettes.Status.Warning),
            E<Label>()
                .Class(StyleClass.StatusBad)
                .FontColor(Palettes.Status.Bad),
            E<Label>()
                .Class(StyleClass.StatusCritical)
                .FontColor(Palettes.Status.Critical),
        ];
    }
}
