using Content.Client.Resources;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SwitchButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, ICheckboxConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        // TODO: make config
        // ICheckboxConfig checkboxCfg = sheet;

        // SwitchButton
        var textureUnchecked = ResCache.GetTexture("/Textures/Interface/Nano/toggleswitch_off.svg.96dpi.png");
        var textureChecked = ResCache.GetTexture("/Textures/Interface/Nano/toggleswitch_on.svg.96dpi.png");
        var textureDisabledUnchecked = ResCache.GetTexture("/Textures/Interface/Nano/toggleswitch_disabled_off.svg.96dpi.png");
        var textureDisabledChecked = ResCache.GetTexture("/Textures/Interface/Nano/toggleswitch_disabled_on.svg.96dpi.png");

        return
        [
            // SwitchButton
            E<SwitchButton>().Prop(SwitchButton.StylePropertySeparation, 10),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>())
                .Prop(TextureRect.StylePropertyTexture, textureUnchecked),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .ParentOf(E<TextureRect>())
                .Prop(TextureRect.StylePropertyTexture, textureChecked),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>())
                .Prop(TextureRect.StylePropertyTexture, textureDisabledUnchecked),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>())
                .Prop(TextureRect.StylePropertyTexture, textureDisabledChecked),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<Label>())
                .Prop(Label.StylePropertyFontColor, sheet.PrimaryPalette.TextDark)
        ];
    }
}
