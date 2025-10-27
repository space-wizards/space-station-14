using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.MainMenu.UI;

[CommonSheetlet]
public sealed class MainMenuSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        return
        [
            // make those buttons bigger
            E<Button>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold)),
            E<BoxContainer>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenuVBox)
                .Prop(BoxContainer.StylePropertySeparation, 2),
        ];
    }
}
