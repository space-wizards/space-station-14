using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Construction.UI;
[Sheetlet]
public sealed class ConstructionMenuSheetlet : ISheetlet<PalettedStylesheet>
{
    public StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<Label>()
                .Identifier("RecipeHistoryNavButtonLabel")
                .Font(sheet.BaseFont.GetFont(8))
                .FontColor(Color.White),

            E<Label>()
                .Identifier("RecipeHistoryNavButtonLabel")
                .PseudoDisabled()
                .Font(sheet.BaseFont.GetFont(8))
                .FontColor(Color.Gray),
        ];
    }
}
