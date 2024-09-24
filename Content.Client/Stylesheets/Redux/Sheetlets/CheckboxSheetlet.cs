using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class CheckboxSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, ICheckboxConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        ICheckboxConfig checkboxCfg = sheet;

        var uncheckedTex = sheet.GetTextureOr(checkboxCfg.CheckboxUncheckedPath, NanotrasenStylesheet.TextureRoot);
        var checkedTex = sheet.GetTextureOr(checkboxCfg.CheckboxCheckedPath, NanotrasenStylesheet.TextureRoot);

        return
        [
            E<TextureRect>()
                .Class(CheckBox.StyleClassCheckBox)
                .Prop(TextureRect.StylePropertyTexture, uncheckedTex),
            E<TextureRect>()
                .Class(CheckBox.StyleClassCheckBox)
                .Class(CheckBox.StyleClassCheckBoxChecked)
                .Prop(TextureRect.StylePropertyTexture, checkedTex),
            E<BoxContainer>()
                .Class(CheckBox.StyleClassCheckBox)
                .Prop(BoxContainer.StylePropertySeparation, 10),
        ];
    }
}
