using Content.Client.Resources;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class SpeechBubbleSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var loocMald = sheet.BaseFont.GetFont(12);

        var nameFont = sheet.BaseFont.GetFont(10);
        var speechFont = sheet.BaseFont.GetFont(12);
        var whisperFont = sheet.BaseFont.GetFont(12, FontKind.Italic);

        var bubbleBackgroundTexture = sheet.ResCache.GetTexture("/Textures/Interface/Nano/chat_bubble_background.png");
        var bubbleBackground = new StyleBoxTexture
        {
            Texture = bubbleBackgroundTexture,
        };
        bubbleBackground.SetPatchMargin(StyleBox.Margin.All, 5);

        return
        [
            E()
                .Class("fontChat")
                .Font(speechFont),

            E()
                .Class("fontChatName")
                .Font(nameFont),

            E()
                .Class("bubblePanel")
                .Panel(bubbleBackground),

            E<PanelContainer>()
                .Class("speechBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFontOutlineThickness, 2f),

            E<PanelContainer>()
                .Class("speechBox", "whisperBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Font(whisperFont),

            E<PanelContainer>()
                .Class("speechBox", "emoteBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Font(whisperFont),

            E<PanelContainer>()
                .Class("speechBox", "maldBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Font(loocMald),

            E<PanelContainer>()
                .Class("speechBox", "nameBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFontOutlineThickness, 2f),

            E<PanelContainer>()
                .Class("nameDivider")
                .ParentOf(E<BoxContainer>())
                .Prop(PanelContainer.StylePropertyPanel,
                    new StyleBoxFlat
                {
                    BackgroundColor = Color.DarkGray,
                    ContentMarginLeftOverride = 2,
                    ContentMarginBottomOverride = 2
                }),
        ];
    }
}
