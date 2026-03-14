using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class ScrollbarSheetlet : Sheetlet<PalettedStylesheet>
{
    public const int DefaultGrabberSize = 10;

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        // TODO: hardcoded colors!!!
        var vScrollBarGrabberNormal = new StyleBoxFlat
        {
            BackgroundColor = Color.Gray.WithAlpha(0.35f), ContentMarginLeftOverride = DefaultGrabberSize,
            ContentMarginTopOverride = DefaultGrabberSize,
        };
        var vScrollBarGrabberHover = new StyleBoxFlat
        {
            BackgroundColor = new Color(140, 140, 140).WithAlpha(0.35f), ContentMarginLeftOverride = DefaultGrabberSize,
            ContentMarginTopOverride = DefaultGrabberSize,
        };

        var vScrollBarGrabberGrabbed = new StyleBoxFlat
        {
            BackgroundColor = new Color(160, 160, 160).WithAlpha(0.35f), ContentMarginLeftOverride = DefaultGrabberSize,
            ContentMarginTopOverride = DefaultGrabberSize,
        };

        var hScrollBarGrabberNormal = new StyleBoxFlat
        {
            BackgroundColor = Color.Gray.WithAlpha(0.35f), ContentMarginTopOverride = DefaultGrabberSize,
        };

        var hScrollBarGrabberHover = new StyleBoxFlat
        {
            BackgroundColor = new Color(140, 140, 140).WithAlpha(0.35f), ContentMarginTopOverride = DefaultGrabberSize,
        };

        var hScrollBarGrabberGrabbed = new StyleBoxFlat
        {
            BackgroundColor = new Color(160, 160, 160).WithAlpha(0.35f), ContentMarginTopOverride = DefaultGrabberSize,
        };

        return
        [
            E<VScrollBar>().Prop(ScrollBar.StylePropertyGrabber, vScrollBarGrabberNormal),
            E<VScrollBar>().PseudoHovered().Prop(ScrollBar.StylePropertyGrabber, vScrollBarGrabberHover),
            E<VScrollBar>().PseudoPressed().Prop(ScrollBar.StylePropertyGrabber, vScrollBarGrabberGrabbed),
            E<HScrollBar>().Prop(ScrollBar.StylePropertyGrabber, hScrollBarGrabberNormal),
            E<HScrollBar>().PseudoHovered().Prop(ScrollBar.StylePropertyGrabber, hScrollBarGrabberHover),
            E<HScrollBar>().PseudoPressed().Prop(ScrollBar.StylePropertyGrabber, hScrollBarGrabberGrabbed),
        ];
    }
}
