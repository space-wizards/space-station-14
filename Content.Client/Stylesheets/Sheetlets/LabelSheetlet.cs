using Content.Client.Resources;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class LabelSheetlet<T> : ISheetlet<T>
    where T : IFontConfig, IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var robotoMonoBold11 = config.MonoFont.GetFont(11, FontKind.Bold);
        var robotoMonoBold12 = config.MonoFont.GetFont(12, FontKind.Bold);
        var robotoMonoBold14 = config.MonoFont.GetFont(14, FontKind.Bold);

        return
        [
            E<Label>()
                .Class(StyleClass.LabelHeading)
                .Font(config.BaseFont.GetFont(16, FontKind.Bold))
                .FontColor(config.HighlightPalette.Text),
            E<Label>()
                .Class(StyleClass.LabelHeadingBigger)
                .Font(config.BaseFont.GetFont(20, FontKind.Bold))
                .FontColor(config.HighlightPalette.Text),
            E<Label>()
                .Class(StyleClass.LabelSubHeading)
                .Font(sheet.BaseFont.GetFont(14, FontKind.Italic))
                .FontColor(sheet.HighlightPalette.TextDark),
            E<Label>()
                .Class(StyleClass.LabelSubText)
                .Font(config.BaseFont.GetFont(10))
                .FontColor(Color.DarkGray),
            E<Label>()
                .Class(StyleClass.LabelKeyText)
                .Font(config.BaseFont.GetFont(12, FontKind.Bold))
                .FontColor(config.HighlightPalette.Text),
            E<Label>()
                .Class(StyleClass.LabelWeak)
                .FontColor(Color.DarkGray), // TODO: you know the drill by now

            E<Label>()
                .Class(StyleClass.Positive)
                .FontColor(config.PositivePalette.Text),
            E<Label>()
                .Class(StyleClass.Negative)
                .FontColor(config.NegativePalette.Text),
            E<Label>()
                .Class(StyleClass.Highlight)
                .FontColor(config.HighlightPalette.Text),

            E<Label>()
                .Class(StyleClass.StatusGood)
                .FontColor(Palettes.Status.Good),
            E<Label>()
                .Class(StyleClass.StatusOkay)
                .FontColor(Palettes.Status.Okay),
            E<Label>()
                .Class(StyleClass.StatusWarning)
                .FontColor(Palettes.Status.Warning),
            E<Label>()
                .Class(StyleClass.StatusBad)
                .FontColor(Palettes.Status.Bad),
            E<Label>()
                .Class(StyleClass.StatusCritical)
                .FontColor(Palettes.Status.Critical),

            // Console text
            E<Label>()
                .Class(StyleClass.LabelMonospaceText)
                .Prop(Label.StylePropertyFont, robotoMonoBold11),
            E<Label>()
                .Class(StyleClass.LabelMonospaceSubHeading)
                .Prop(Label.StylePropertyFont, robotoMonoBold12),
            E<Label>()
                .Class(StyleClass.LabelMonospaceHeading)
                .Prop(Label.StylePropertyFont, robotoMonoBold14),
        ];
    }
}
