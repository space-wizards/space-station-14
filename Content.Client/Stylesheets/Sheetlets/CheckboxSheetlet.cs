using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet]
public sealed class CheckboxSheetlet<T> : ISheetlet<T> where T : PalettedStylesheet, ICheckboxConfig
{
    public StyleRule[] GetRules(T sheet, object config)
    {
        ICheckboxConfig checkboxCfg = sheet;

        var uncheckedTex = sheet.GetTextureOr(checkboxCfg.CheckboxUncheckedPath, NanotrasenStylesheetFactory.TextureRoot);
        var checkedTex = sheet.GetTextureOr(checkboxCfg.CheckboxCheckedPath, NanotrasenStylesheetFactory.TextureRoot);

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
