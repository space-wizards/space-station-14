using Content.Client.Stylesheets.Redux.Palette;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class HLineSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<HLine>()
                .Class(StyleClass.Positive)
                .Panel(new StyleBoxFlat(sheet.PositivePalette.Text)),
            E<HLine>()
                .Class(StyleClass.Highlight)
                .Panel(new StyleBoxFlat(sheet.HighlightPalette.Text)),
            E<HLine>()
                .Class(StyleClass.Negative)
                .Panel(new StyleBoxFlat(sheet.NegativePalette.Text)),
        ];
    }
}
