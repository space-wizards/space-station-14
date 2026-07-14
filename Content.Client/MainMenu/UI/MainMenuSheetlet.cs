using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.MainMenu.UI;

[Sheetlet(typeof(NanotrasenStylesheetDefinition))]
public sealed class MainMenuSheetlet<T> : ISheetlet<T>
    where T : IFontConfig
{
    public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
    {
        return
        [
            // make those buttons bigger
            E<Button>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .ParentOf(E<Label>())
                .Font(config.BaseFont.GetFont(16, FontKind.Bold)),
            E<BoxContainer>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenuVBox)
                .Prop(BoxContainer.StylePropertySeparation, 2),
        ];
    }
}
