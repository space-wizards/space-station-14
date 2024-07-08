using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class ItemListSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var itemListBackgroundSelected = new StyleBoxFlat(new Color(75, 75, 86));
        itemListBackgroundSelected.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        itemListBackgroundSelected.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        var itemListItemBackgroundDisabled = new StyleBoxFlat(new Color(10, 10, 12));
        itemListItemBackgroundDisabled.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        itemListItemBackgroundDisabled.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        var itemListItemBackground = new StyleBoxFlat(new Color(55, 55, 68));
        itemListItemBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        itemListItemBackground.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        var itemListItemBackgroundTransparent = new StyleBoxFlat(Color.Transparent);
        itemListItemBackgroundTransparent.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        itemListItemBackgroundTransparent.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        return
        [
            E<ItemList>()
                .Prop(ItemList.StylePropertyBackground,
                    new StyleBoxFlat {BackgroundColor = new Color(32, 32, 40)})
                .Prop(ItemList.StylePropertyItemBackground,
                    itemListItemBackground)
                .Prop(ItemList.StylePropertyDisabledItemBackground,
                    itemListItemBackgroundDisabled)
                .Prop(ItemList.StylePropertySelectedItemBackground,
                    itemListBackgroundSelected),

            E<ItemList>().Class("transparentItemList")
                .Prop(ItemList.StylePropertyBackground,
                    itemListItemBackgroundTransparent)
                .Prop(ItemList.StylePropertyItemBackground,
                    itemListItemBackgroundTransparent)
                .Prop(ItemList.StylePropertyDisabledItemBackground,
                    itemListItemBackgroundDisabled)
                .Prop(ItemList.StylePropertySelectedItemBackground,
                    itemListBackgroundSelected),

            E<ItemList>().Class("transparentBackgroundItemList")
                .Prop(ItemList.StylePropertyBackground,
                    itemListItemBackgroundTransparent)
                .Prop(ItemList.StylePropertyItemBackground,
                    itemListItemBackground)
                .Prop(ItemList.StylePropertyDisabledItemBackground,
                    itemListItemBackgroundDisabled)
                .Prop(ItemList.StylePropertySelectedItemBackground,
                    itemListBackgroundSelected),
        ];
    }
}
