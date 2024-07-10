using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class StripebackSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var stripebackCfg = (IStripebackConfig) sheet;
        var stripeBack = new StyleBoxTexture
        {
            Texture = sheet.GetTexture(stripebackCfg.StripebackPath),
            Mode = StyleBoxTexture.StretchMode.Tile,
        };

        return new StyleRule[]
        {
            E<StripeBack>()
                .Prop(StripeBack.StylePropertyBackground, stripeBack),
        };
    }
}
