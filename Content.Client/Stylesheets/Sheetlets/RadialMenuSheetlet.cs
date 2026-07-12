using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class RadialMenuSheetlet<T> : ISheetlet<T>
    where T : IRadialMenuConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var btnNormalTex = factory.GetTexture(config.ButtonNormalPath);
        var btnHoverTex = factory.GetTexture(config.ButtonHoverPath);
        var closeNormalTex = factory.GetTexture(config.CloseNormalPath);
        var closeHoverTex = factory.GetTexture(config.CloseHoverPath);
        var backNormalTex = factory.GetTexture(config.BackNormalPath);
        var backHoverTex = factory.GetTexture(config.BackHoverPath);

        return
        [
            // TODO: UNHARDCODE
            E<TextureButton>()
                .Class("RadialMenuButton")
                .Prop(TextureButton.StylePropertyTexture, btnNormalTex),
            E<TextureButton>()
                .Class("RadialMenuButton")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, btnHoverTex),

            E<TextureButton>()
                .Class("RadialMenuCloseButton")
                .Prop(TextureButton.StylePropertyTexture, closeNormalTex),
            E<TextureButton>()
                .Class("RadialMenuCloseButton")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, closeHoverTex),

            E<TextureButton>()
                .Class("RadialMenuBackButton")
                .Prop(TextureButton.StylePropertyTexture, backNormalTex),
            E<TextureButton>()
                .Class("RadialMenuBackButton")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, backHoverTex),
        ];
    }
}
