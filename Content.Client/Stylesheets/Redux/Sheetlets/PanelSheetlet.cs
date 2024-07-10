using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class PanelSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var panelPalette = (IPanelPalette) sheet;
        var buttonCfg = (IButtonConfig) sheet;

        var boxLight = new StyleBoxFlat()
        {
            BackgroundColor = panelPalette.PanelLightColor,
        };
        var boxDark = new StyleBoxFlat()
        {
            BackgroundColor = panelPalette.PanelDarkColor,
        };
        var boxDivider = new StyleBoxFlat
        {
            BackgroundColor = sheet.HighlightPalette.Base,
            ContentMarginBottomOverride = 2,
            ContentMarginLeftOverride = 2,
        };
        var boxPositive = new StyleBoxFlat { BackgroundColor = sheet.PositivePalette.Background };
        var boxNegative = new StyleBoxFlat { BackgroundColor = sheet.NegativePalette.Background };
        var boxHighlight = new StyleBoxFlat { BackgroundColor = sheet.HighlightPalette.Background };

        return
        [
            E<PanelContainer>().Class(StyleClass.PanelLight).Panel(boxLight),
            E<PanelContainer>().Class(StyleClass.PanelDark).Panel(boxDark),
            E<PanelContainer>().Class(StyleClass.HighDivider).Panel(boxDivider),

            E<PanelContainer>().Class(StyleClass.Positive).Panel(boxPositive),
            E<PanelContainer>().Class(StyleClass.Negative).Panel(boxNegative),
            E<PanelContainer>().Class(StyleClass.Highlight).Panel(boxHighlight),

            E()
                .Class(StyleClass.BackgroundPanel)
                .Prop(PanelContainer.StylePropertyPanel, buttonCfg.ConfigureBaseButton(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelOpenLeft)
                .Prop(PanelContainer.StylePropertyPanel, buttonCfg.ConfigureOpenLeftButton(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelOpenRight)
                .Prop(PanelContainer.StylePropertyPanel, buttonCfg.ConfigureOpenRightButton(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
        ];
    }
}
