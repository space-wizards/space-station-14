using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class CheckboxSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var checkboxCfg = (ICheckboxConfig) sheet;
        return
        [
            E<TextureRect>()
                .Class(CheckBox.StyleClassCheckBox)
                .Prop(TextureRect.StylePropertyTexture, sheet.GetTexture(checkboxCfg.CheckboxUncheckedPath)),
            E<TextureRect>()
                .Class(CheckBox.StyleClassCheckBox)
                .Class(CheckBox.StyleClassCheckBoxChecked)
                .Prop(TextureRect.StylePropertyTexture, sheet.GetTexture(checkboxCfg.CheckboxCheckedPath)),
            E<BoxContainer>()
                .Class(CheckBox.StyleClassCheckBox)
                .Prop(BoxContainer.StylePropertySeparation, 10),
        ];
    }
}
