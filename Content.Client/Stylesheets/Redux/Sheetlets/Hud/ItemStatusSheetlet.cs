using Content.Client.Stylesheets.Redux.Fonts;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ItemStatusSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return new StyleRule[]
        {
            Element().Class(StyleClass.StyleClassItemStatus)
                .Prop("font", sheet.BaseFont.GetFont(10)),

            Element().Class(StyleClass.StyleClassItemStatusNotHeld)
                .Prop("font", sheet.BaseFont.GetFont(10, FontStack.FontKind.Italic))
                .Prop("font-color", Color.Gray),

            Element<RichTextLabel>().Class(StyleClass.StyleClassItemStatus)
                .Prop(nameof(RichTextLabel.LineHeightScale), 0.7f)
                .Prop(nameof(Control.Margin), new Thickness(0, 0, 0, -6)),
        };
    }
}
