using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class StripebackSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IStripebackConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IStripebackConfig stripebackCfg = sheet;

        var stripeBack = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(stripebackCfg.StripebackPath, NanotrasenStylesheet.TextureRoot),
            Mode = StyleBoxTexture.StretchMode.Tile,
        };

        return
        [
            E<StripeBack>()
                .Prop(StripeBack.StylePropertyBackground, stripeBack),
        ];
    }
}
