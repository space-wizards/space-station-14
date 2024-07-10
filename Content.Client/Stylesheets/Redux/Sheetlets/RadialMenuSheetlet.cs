using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class RadialMenuSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var radialCfg = (IRadialMenuConfig) sheet;

        return
        [
            // TODO: UNHARDCODE
            E<TextureButton>()
                .Class("RadialMenuButton")
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(radialCfg.ButtonNormalPath)),
            E<TextureButton>()
                .Class("RadialMenuButton")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(radialCfg.ButtonHoverPath)),

            E<TextureButton>()
                .Class("RadialMenuCloseButton")
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(radialCfg.CloseNormalPath)),
            E<TextureButton>()
                .Class("RadialMenuCloseButton")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(radialCfg.CloseHoverPath)),

            E<TextureButton>()
                .Class("RadialMenuBackButton")
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(radialCfg.ButtonNormalPath)),
            E<TextureButton>()
                .Class("RadialMenuBackButton")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(radialCfg.ButtonHoverPath)),
        ];
    }
}
