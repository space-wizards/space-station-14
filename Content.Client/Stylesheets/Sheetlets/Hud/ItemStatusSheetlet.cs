using Content.Client.Stylesheets.Fonts;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ItemStatusSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E()
                .Class(StyleClass.ItemStatus)
                .Prop("font", sheet.BaseFont.GetFont(10)),

            E()
                .Class(StyleClass.ItemStatusNotHeld)
                .Prop("font", sheet.BaseFont.GetFont(10, FontKind.Italic))
                .Prop("font-color", Color.Gray),

            E<RichTextLabel>()
                .Class(StyleClass.ItemStatus)
                .Prop(nameof(RichTextLabel.LineHeightScale), 0.7f)
                .Prop(nameof(Control.Margin), new Thickness(0, 0, 0, -6)),
        ];
    }
}
