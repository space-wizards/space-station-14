using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ChatSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var chatColor = sheet.SecondaryPalette[3].WithAlpha(221.0f / 255.0f);

        var chatBg = new StyleBoxFlat(chatColor);

        return
        [
            E<PanelContainer>()
                .Class(StyleClass.ChatPanel)
                .Panel(chatBg),
            E<LineEdit>()
                .Class(StyleClass.ChatLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),
        ];
    }
}
