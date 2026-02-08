using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class ProgressBarSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var progressBarBackground = new StyleBoxFlat
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundDark,
        };
        progressBarBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 14.5f);
        var progressBarForeground = new StyleBoxFlat
        {
            BackgroundColor = sheet.PositivePalette.Element,
        };
        progressBarForeground.SetContentMarginOverride(StyleBox.Margin.Vertical, 14.5f);

        return
        [
            E<ProgressBar>()
                .Prop(ProgressBar.StylePropertyBackground, progressBarBackground)
                .Prop(ProgressBar.StylePropertyForeground, progressBarForeground),
        ];
    }
}
