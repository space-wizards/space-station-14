using Content.Client.Resources;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

/// These are not in `LabelSheetlet` because a label is not the only thing you might want to be monospaced.
[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class TextSheetlet<T> : ISheetlet<T>
    where T : IFontConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var mono = config.MonoFont.GetFont(12);

        return
        [
            E().Class(StyleClass.Monospace).Font(mono),
            E().Class(StyleClass.Italic).Font(config.BaseFont.GetFont(12, FontKind.Italic)),
            E().Class(StyleClass.FontLarge).Font(config.BaseFont.GetFont(14)),
            E().Class(StyleClass.FontSmall).Font(config.BaseFont.GetFont(10)),
        ];
    }
}
