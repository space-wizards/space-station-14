using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class PanelSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonCfg = sheet;

        var boxLight = new StyleBoxFlat()
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundLight,
        };
        var boxDark = new StyleBoxFlat()
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundDark,
        };
        var boxPositive = new StyleBoxFlat { BackgroundColor = sheet.PositivePalette.Background };
        var boxNegative = new StyleBoxFlat { BackgroundColor = sheet.NegativePalette.Background };
        var boxHighlight = new StyleBoxFlat { BackgroundColor = sheet.HighlightPalette.Background };

        return
        [
            E<PanelContainer>().Class(StyleClass.PanelLight).Panel(boxLight),
            E<PanelContainer>().Class(StyleClass.PanelDark).Panel(boxDark),

            E<PanelContainer>().Class(StyleClass.Positive).Panel(boxPositive),
            E<PanelContainer>().Class(StyleClass.Negative).Panel(boxNegative),
            E<PanelContainer>().Class(StyleClass.Highlight).Panel(boxHighlight),

            // TODO: this should probably be cleaned up but too many UIs rely on this hardcoded color so I'm scared to touch it
            E<PanelContainer>()
                .Class("BackgroundDark")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(Color.FromHex("#25252A"))),

            // panels that have the same corner bezels as buttons
            E()
                .Class(StyleClass.BackgroundPanel)
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.BaseStyleBox(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelOpenLeft)
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.OpenLeftStyleBox(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelOpenRight)
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.OpenRightStyleBox(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
        ];
    }
}
