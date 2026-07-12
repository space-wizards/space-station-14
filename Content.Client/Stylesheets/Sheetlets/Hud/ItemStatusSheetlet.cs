using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class ItemStatusSheetlet<T> : ISheetlet<T>
    where T : IFontConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        return
        [
            E()
                .Class(StyleClass.ItemStatus)
                .Prop("font", config.BaseFont.GetFont(10)),

            E()
                .Class(StyleClass.ItemStatusNotHeld)
                .Prop("font", config.BaseFont.GetFont(10, FontKind.Italic))
                .Prop("font-color", Color.Gray),

            E<RichTextLabel>()
                .Class(StyleClass.ItemStatus)
                .Prop(nameof(RichTextLabel.LineHeightScale), 0.7f)
                .Prop(nameof(Control.Margin), new Thickness(0, 0, 0, -6)),
        ];
    }
}
