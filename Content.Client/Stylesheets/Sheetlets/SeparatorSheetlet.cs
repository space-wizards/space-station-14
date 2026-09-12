using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SeparatorSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<Separator>().Class(StyleClass.LowDivider)
                .Prop(Separator.StylePropertyColor, sheet.SecondaryPalette.TextDark),
            E<Separator>().Class(StyleClass.HighDivider)
                .Prop(Separator.StylePropertyColor, sheet.HighlightPalette.Base),
            E<Separator>().Class(StyleClass.Positive)
                .Prop(Separator.StylePropertyColor, sheet.PositivePalette.Text),
            E<Separator>().Class(StyleClass.Highlight)
                .Prop(Separator.StylePropertyColor, sheet.HighlightPalette.Text),
            E<Separator>().Class(StyleClass.Negative)
                .Prop(Separator.StylePropertyColor, sheet.NegativePalette.Text),
        ];
    }
}
