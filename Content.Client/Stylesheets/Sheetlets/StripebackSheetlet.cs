using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class StripebackSheetlet<T> : ISheetlet<T>
    where T : IStripebackConfig
{
    public StyleRule[] GetRules(StylesheetFactory sheet, T config)
    {
        var stripeBack = new StyleBoxTexture
        {
            Texture = sheet.GetTexture(config.StripebackPath),
            Mode = StyleBoxTexture.StretchMode.Tile,
        };

        return
        [
            E<StripeBack>()
                .Prop(StripeBack.StylePropertyBackground, stripeBack),
        ];
    }
}
