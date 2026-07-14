using Content.Client.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Construction.UI;

[Sheetlet(typeof(CommonStylesheetDefinition))]
public sealed class ConstructionMenuSheetlet<T> : ISheetlet<T>
    where T : IFontConfig
{
    public StyleRule[] GetRules(StylesheetDefinition resolver, T config)
    {
        return
        [
            E<Label>()
                .Identifier("RecipeHistoryNavButtonLabel")
                .Font(config.BaseFont.GetFont(8))
                .FontColor(Color.White),

            E<Label>()
                .Identifier("RecipeHistoryNavButtonLabel")
                .PseudoDisabled()
                .Font(config.BaseFont.GetFont(8))
                .FontColor(Color.Gray),
        ];
    }
}
