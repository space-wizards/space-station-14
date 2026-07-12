using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class ItemListSheetlet<T> : ISheetlet<T>
    where T : IPaletteConfig
{
    private static StyleBoxFlat Box(Color c)
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

    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var boxBackground = new StyleBoxFlat { BackgroundColor = config.PrimaryPalette.Background };
        var boxItemBackground = Box(config.PrimaryPalette.Background);
        var boxSelected = Box(config.PrimaryPalette.Element);
        var boxDisabled = Box(config.PrimaryPalette.BackgroundDark);

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
