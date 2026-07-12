using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class ProgressBarSheetlet<T> : ISheetlet<T>
    where T : ISheetletConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        // TODO: 1) hardcoded colors, 2) yuck
        var progressBarBackground = new StyleBoxFlat
        {
            BackgroundColor = new Color(0.25f, 0.25f, 0.25f),
        };
        progressBarBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 14.5f);
        var progressBarForeground = new StyleBoxFlat
        {
            BackgroundColor = new Color(0.25f, 0.50f, 0.25f),
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
