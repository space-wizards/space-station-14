using Content.Client.Stylesheets.Redux.SheetletConfig;
using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets.Redux.NTSheetlets;

public sealed class NTButtonSheetlet : ButtonSheetlet
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var cfg = (IButtonConfig) sheet;
        var rules = new List<StyleRule>(base.GetRules(sheet, config));

        MakeButtonRules(cfg, rules, cfg.ButtonPalette, null);
        MakeButtonRules(cfg, rules, cfg.PositiveButtonPalette, StyleClass.Positive);
        MakeButtonRules(cfg, rules, cfg.NegativeButtonPalette, StyleClass.Negative);

        return rules.ToArray();
    }
}
