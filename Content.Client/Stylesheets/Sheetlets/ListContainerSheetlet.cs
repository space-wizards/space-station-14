using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetDefinition))]
public sealed class ListContainerSheetlet<T> : ISheetlet<T>
    where T : IButtonConfig, IIconConfig, IPaletteConfig, IFontConfig
{
    public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
    {
        var box = new StyleBoxFlat() { BackgroundColor = Color.White };

        var rules = new List<StyleRule>(
        [
            E<ContainerButton>()
                .Class(ListContainer.StyleClassListContainerButton)
                .Box(box),
        ]);
        ButtonSheetlet<T>.MakeButtonRules<ContainerButton>(rules,
            config.ButtonPalette,
            ListContainer.StyleClassListContainerButton);

        return rules.ToArray();
    }
}
