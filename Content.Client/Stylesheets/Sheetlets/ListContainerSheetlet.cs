using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class ListContainerSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonCfg = sheet;

        var box = new StyleBoxFlat() { BackgroundColor = Color.White };

        var rules = new List<StyleRule>(
        [
            E<ContainerButton>()
                .Class(ListContainer.StyleClassListContainerButton)
                .Box(box),
        ]);
        ButtonSheetlet<T>.MakeButtonRules<ContainerButton>(rules,
            buttonCfg.ButtonPalette,
            ListContainer.StyleClassListContainerButton);

        return rules.ToArray();
    }
}
