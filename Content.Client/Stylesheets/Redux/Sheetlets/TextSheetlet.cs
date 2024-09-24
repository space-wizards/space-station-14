using Content.Client.Stylesheets.Redux.Fonts;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

/// These are not in `LabelSheetlet` because a label is not the only thing you might want to be monospaced.
[CommonSheetlet]
public sealed class TextSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var notoMono = new SingleFontFamily(ResCache, "/EngineFonts/NotoSans/NotoSansMono-Regular.ttf");

        return
        [
            E().Class(StyleClass.Monospace).Font(notoMono.GetFont(12)),
            E().Class(StyleClass.Italic).Font(sheet.BaseFont.GetFont(12, FontKind.Italic)),
            E().Class(StyleClass.FontLarge).Font(sheet.BaseFont.GetFont(14)),
            E().Class(StyleClass.FontSmall).Font(sheet.BaseFont.GetFont(10)),
        ];
    }
}
