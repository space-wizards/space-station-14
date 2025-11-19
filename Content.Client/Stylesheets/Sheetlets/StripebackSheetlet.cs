using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

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
