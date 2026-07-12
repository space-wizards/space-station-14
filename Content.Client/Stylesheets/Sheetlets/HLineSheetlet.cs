using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class HLineSheetlet<T> : ISheetlet<T>
    where T : IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        return
        [
            E<HLine>()
                .Class(StyleClass.Positive)
                .Panel(new StyleBoxFlat(config.PositivePalette.Text)),
            E<HLine>()
                .Class(StyleClass.Highlight)
                .Panel(new StyleBoxFlat(config.HighlightPalette.Text)),
            E<HLine>()
                .Class(StyleClass.Negative)
                .Panel(new StyleBoxFlat(config.NegativePalette.Text)),
        ];
    }
}
