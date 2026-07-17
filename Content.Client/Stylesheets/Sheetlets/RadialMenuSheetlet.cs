using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class RadialMenuSheetlet<T> : Sheetlet<T> where T: PalettedStylesheet, IRadialMenuConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IRadialMenuConfig radialCfg = sheet;

        var btnNormalTex = sheet.GetTextureOr(radialCfg.ButtonNormalPath, NanotrasenStylesheet.TextureRoot);
        var btnHoverTex = sheet.GetTextureOr(radialCfg.ButtonHoverPath, NanotrasenStylesheet.TextureRoot);
        var closeNormalTex = sheet.GetTextureOr(radialCfg.CloseNormalPath, NanotrasenStylesheet.TextureRoot);
        var closeHoverTex = sheet.GetTextureOr(radialCfg.CloseHoverPath, NanotrasenStylesheet.TextureRoot);
        var backNormalTex = sheet.GetTextureOr(radialCfg.BackNormalPath, NanotrasenStylesheet.TextureRoot);
        var backHoverTex = sheet.GetTextureOr(radialCfg.BackHoverPath, NanotrasenStylesheet.TextureRoot);

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
