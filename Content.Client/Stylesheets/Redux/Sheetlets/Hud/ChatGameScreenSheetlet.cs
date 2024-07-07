using Content.Client.Stylesheets.Redux.NTSheetlets;
using Content.Client.UserInterface.Screens;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ChatGameScreenSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var panel = (IPanelPalette) sheet;
        return
        [
            Element()
                .Class(SeparatedChatGameScreen.StyleClassChatContainer)
                .Panel(new StyleBoxFlat(panel.BackingPanelPalette)),
            E<OutputPanel>()
                .Class(SeparatedChatGameScreen.StyleClassChatOutput)
                .Panel(new StyleBoxFlat(sheet.SecondaryPalette[4])),
        ];
    }
}
