using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class DividersSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var boxHighDivider = new StyleBoxFlat
        {
            BackgroundColor = sheet.HighlightPalette.Base,
            ContentMarginBottomOverride = 2,
            ContentMarginLeftOverride = 2,
        };

        var boxLowDivider = new StyleBoxFlat(sheet.SecondaryPalette.TextDark);

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
