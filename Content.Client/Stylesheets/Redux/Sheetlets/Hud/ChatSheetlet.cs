using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ChatSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var chatColor = sheet.SecondaryPalette[3].WithAlpha(221.0f / 255.0f);

        var chatBg = new StyleBoxFlat(chatColor);

        return new StyleRule[]
        {
            E<PanelContainer>().Class(StyleClasses.ChatPanel)
                .Panel(chatBg),
            E<LineEdit>().Class(StyleClasses.ChatLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),
        };
    }
}
