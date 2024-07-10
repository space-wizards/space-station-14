using Content.Client.Stylesheets.Redux.SheetletConfig;
using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.NTSheetlets;

public sealed class NTPanelSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var cfg = (IButtonConfig) sheet;

        var rules = new List<StyleRule>
        {
            E()
                .Class(StyleClass.BackgroundPanel)
                .Prop(PanelContainer.StylePropertyPanel, cfg.ConfigureBaseButton(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelOpenLeft)
                .Prop(PanelContainer.StylePropertyPanel, cfg.ConfigureOpenLeftButton(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelOpenRight)
                .Prop(PanelContainer.StylePropertyPanel, cfg.ConfigureOpenRightButton(sheet))
                .Modulate(sheet.SecondaryPalette.Background),
        };

        return rules.ToArray();
    }
}
