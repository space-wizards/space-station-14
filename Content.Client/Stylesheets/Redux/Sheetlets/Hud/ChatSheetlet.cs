using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ChatSheetlet<T> : Sheetlet<T> where T: PalettedStylesheet, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig btnCfg = sheet;

        var chatColor = sheet.SecondaryPalette.Background.WithAlpha(221.0f / 255.0f);
        var chatBg = new StyleBoxFlat(chatColor);

        var chatChannelButtonTex =
            sheet.GetTextureOr(btnCfg.RoundedButtonBorderedPath, NanotrasenStylesheet.TextureRoot);
        var chatChannelButton = new StyleBoxTexture
        {
            Texture = chatChannelButtonTex,
        };
        chatChannelButton.SetPatchMargin(StyleBox.Margin.All, 5);
        chatChannelButton.SetPadding(StyleBox.Margin.All, 2);

        var chatFilterButtonTex =
            sheet.GetTextureOr(btnCfg.RoundedButtonBorderedPath, NanotrasenStylesheet.TextureRoot);
        var chatFilterButton = new StyleBoxTexture
        {
            Texture = chatFilterButtonTex,
        };
        chatFilterButton.SetPatchMargin(StyleBox.Margin.All, 5);
        chatFilterButton.SetPadding(StyleBox.Margin.All, 2);

        return
        [
            E<PanelContainer>()
                .Class(ChatInputBox.StyleClassChatPanel)
                .Panel(chatBg),
            E<LineEdit>()
                .Class(ChatInputBox.StyleClassChatLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),
            E<Button>().Class(ChatInputBox.StyleClassChatFilterOptionButton).Box(chatChannelButton),
            E<ContainerButton>().Class(ChatInputBox.StyleClassChatFilterOptionButton).Box(chatFilterButton),
        ];
    }
}
