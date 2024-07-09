using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class ItemListSheetlet : Sheetlet<PalettedStylesheet>
{
    private StyleBoxFlat Box(Color c)
    {
        return new StyleBoxFlat(c)
            // TODO: dont hardcode these maybe
            {
                ContentMarginLeftOverride = 4,
                ContentMarginTopOverride = 2,
                ContentMarginRightOverride = 4,
                ContentMarginBottomOverride = 2,
            };
    }

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var boxBackground = new StyleBoxFlat { BackgroundColor = sheet.SecondaryPalette[3] };
        var boxTransparent = new StyleBoxFlat(Color.Transparent);
        var boxItemBackground = Box(sheet.SecondaryPalette[2]);
        var boxSelected = Box(sheet.SecondaryPalette[3]);
        var boxDisabled = Box(sheet.SecondaryPalette[4]);

        return
        [
            E<ItemList>()
                .Prop(ItemList.StylePropertyBackground, boxBackground)
                .Prop(ItemList.StylePropertyItemBackground, boxItemBackground)
                .Prop(ItemList.StylePropertyDisabledItemBackground, boxDisabled)
                .Prop(ItemList.StylePropertySelectedItemBackground, boxSelected),

            // these styles seem to be unused now
            // E<ItemList>().Class("transparentItemList")
            //     .Prop(ItemList.StylePropertyBackground, boxTransparent)
            //     .Prop(ItemList.StylePropertyItemBackground, boxTransparent)
            //     .Prop(ItemList.StylePropertyDisabledItemBackground, boxDisabled)
            //     .Prop(ItemList.StylePropertySelectedItemBackground, boxItemBackground),
            //
            // E<ItemList>().Class("transparentBackgroundItemList")
            //     .Prop(ItemList.StylePropertyBackground, boxTransparent)
            //     .Prop(ItemList.StylePropertyItemBackground, boxBackground)
            //     .Prop(ItemList.StylePropertyDisabledItemBackground, boxItemBackground)
            //     .Prop(ItemList.StylePropertySelectedItemBackground, boxSelected),
        ];
    }
}
