using Content.Client.Resources;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class MonotoneCheckBoxSheetlet<T> : ISheetlet<T>
    where T : IButtonConfig
{
    public StyleRule[] GetRules(StylesheetFactory sheet, T config)
    {
        var monotoneCheckBoxTextureChecked =
            sheet.GetTexture(
                new ResPath("Monotone/monotone_checkbox_checked.svg.96dpi.png"));
        var monotoneCheckBoxTextureUnchecked =
            sheet.GetTexture(
                new ResPath("Monotone/monotone_checkbox_unchecked.svg.96dpi.png"));

        return
        [
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
