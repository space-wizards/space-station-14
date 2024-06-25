using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.NTSheetlets;

public sealed class NTPanelSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var cfg = (IButtonConfig) sheet;

        var rules = new List<StyleRule>
        {
            Element().Class(StyleClass.BackgroundPanel)
                .Prop(PanelContainer.StylePropertyPanel, cfg.ConfigureBaseButton(sheet))
                .Modulate(sheet.SecondaryPalette[3]),
            Element().Class(StyleClass.BackgroundPanelOpenLeft)
                .Prop(PanelContainer.StylePropertyPanel, cfg.ConfigureOpenLeftButton(sheet))
                .Modulate(sheet.SecondaryPalette[3]),
            Element().Class(StyleClass.BackgroundPanelOpenRight)
                .Prop(PanelContainer.StylePropertyPanel, cfg.ConfigureOpenRightButton(sheet))
                .Modulate(sheet.SecondaryPalette[3]),
        };

        return rules.ToArray();
    }
}
