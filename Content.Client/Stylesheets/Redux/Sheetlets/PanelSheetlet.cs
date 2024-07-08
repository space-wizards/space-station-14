using Content.Client.Stylesheets.Redux.SheetletConfig;
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
        var cfg = (IPanelPalette) sheet;

        var boxLight = new StyleBoxFlat()
        {
            BackgroundColor = cfg.PanelLightColor
        };
        var boxDark = new StyleBoxFlat()
        {
            BackgroundColor = cfg.PanelDarkColor
        };
        var boxDivider = new StyleBoxFlat
        {
            BackgroundColor = sheet.HighlightPalette[0],
            ContentMarginBottomOverride = 2,
            ContentMarginLeftOverride = 2,
        };

        return
        [
            E<PanelContainer>().Class(StyleClass.PanelLight).Panel(boxLight),
            E<PanelContainer>().Class(StyleClass.PanelDark).Panel(boxDark),
            E<PanelContainer>().Class(StyleClass.HighDivider).Panel(boxDivider),
        ];
    }
}
