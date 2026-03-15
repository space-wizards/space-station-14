using Content.Client.Resources;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

/// These are not in `LabelSheetlet` because a label is not the only thing you might want to be monospaced.
[CommonSheetlet]
public sealed class TextSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var mono = sheet.Fonts.GetFont(StandardFontType.Monospace, 12);

        return
        [
            E().Class(StyleClass.Monospace).Font(mono),
            E().Class(StyleClass.Italic).Font(sheet.Fonts.GetFont(StandardFontType.Main, 12, FontKind.Italic)),
            E().Class(StyleClass.FontLarge).Font(sheet.Fonts.GetFont(StandardFontType.Main, 14)),
            E().Class(StyleClass.FontSmall).Font(sheet.Fonts.GetFont(StandardFontType.Main, 10)),
        ];
    }
}
