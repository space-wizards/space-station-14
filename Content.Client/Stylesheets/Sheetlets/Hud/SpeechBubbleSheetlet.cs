using Content.Client.Stylesheets.Fonts;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;


namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class SpeechBubbleSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var medium = sheet.BaseFont.GetFont(12);

        var whisper = sheet.BaseFont.GetFont(12, FontKind.Italic);

        return
        [
            E()
                .Class("fontChat")
                .Font(medium),

            E<PanelContainer>()
                .Class("speechBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFontOutlineThickness, 2f),

            E<PanelContainer>()
                .Class("speechBox", "whisperBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Font(whisper),

            E<PanelContainer>()
                .Class("speechBox", "emoteBox")
                .ParentOf(E<BoxContainer>())
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontKind.Italic)),
        ];
    }
}
