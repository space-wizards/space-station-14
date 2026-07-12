using System.Numerics;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class DividersSheetlet<T> : ISheetlet<T>
    where T : IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var boxHighDivider = new StyleBoxFlat
        {
            BackgroundColor = config.HighlightPalette.Base,
            ContentMarginBottomOverride = 2,
            ContentMarginLeftOverride = 2,
        };

        var boxLowDivider = new StyleBoxFlat(config.SecondaryPalette.TextDark);

        // high divider and low divider styles are VERY inconsistent but its too much of a pain to change right now (also HighDivider has its own Control ???)
        // i dont think theres a good resolution to this besides just deleting HighDivider. HighDivider is barely used but LowDivider is used everywhere.
        return
        [
            E<PanelContainer>()
                .Class(StyleClass.LowDivider)
                .Panel(boxLowDivider)
                .MinSize(new Vector2(2, 2)),
            E<PanelContainer>().Class(StyleClass.HighDivider).Panel(boxHighDivider),
        ];
    }
}
