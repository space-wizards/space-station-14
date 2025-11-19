using Content.Client.Resources;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class MonotoneCheckBoxSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonCfg = sheet;

        var monotoneCheckBoxTextureChecked = ResCache.GetTexture("/Textures/Interface/Nano/Monotone/monotone_checkbox_checked.svg.96dpi.png");
        var monotoneCheckBoxTextureUnchecked = ResCache.GetTexture("/Textures/Interface/Nano/Monotone/monotone_checkbox_unchecked.svg.96dpi.png");

        return [
            E<TextureRect>()
                .Class(MonotoneCheckBox.StyleClassMonotoneCheckBox)
                .Prop(TextureRect.StylePropertyTexture, monotoneCheckBoxTextureUnchecked),
            E<TextureRect>()
                .Class(MonotoneCheckBox.StyleClassMonotoneCheckBox)
                .Class(CheckBox.StyleClassCheckBoxChecked)
                .Prop(TextureRect.StylePropertyTexture, monotoneCheckBoxTextureChecked),
        ];
    }
}
