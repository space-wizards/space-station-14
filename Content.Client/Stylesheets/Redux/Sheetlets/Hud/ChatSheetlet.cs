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

        var chatChannelButtonTex = sheet.GetTexture("rounded_button.svg.96dpi.png");
        var chatChannelButton = new StyleBoxTexture
        {
            Texture = chatChannelButtonTex,
        };
        chatChannelButton.SetPatchMargin(StyleBox.Margin.All, 5);
        chatChannelButton.SetPadding(StyleBox.Margin.All, 2);

        var chatFilterButtonTex = sheet.GetTexture("rounded_button_bordered.svg.96dpi.png");
        var chatFilterButton = new StyleBoxTexture
        {
            Texture = chatFilterButtonTex,
        };
        chatFilterButton.SetPatchMargin(StyleBox.Margin.All, 5);
        chatFilterButton.SetPadding(StyleBox.Margin.All, 2);

        return
        [
            E<PanelContainer>()
                .Class(StyleClass.ChatPanel)
                .Panel(chatBg),
            E<LineEdit>()
                .Class(StyleClass.ChatLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),
            E<Button>().Class(StyleClass.ChatFilterOptionButton).Box(chatChannelButton),
            E<ContainerButton>().Class(StyleClass.ChatFilterOptionButton).Box(chatFilterButton),
        ];
    }
}
