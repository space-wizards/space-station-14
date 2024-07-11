using Content.Client.Examine;
using Content.Client.Stylesheets.Redux.Fonts;
using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class TooltipSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var tooltipCfg = (ITooltipConfig) sheet;

        var tooltipBox = sheet.GetTextureOr(tooltipCfg.TooltipBoxPath, NanotrasenStylesheet.TextureRoot).IntoPatch(StyleBox.Margin.All, 2);
        tooltipBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

        var whisperBox = sheet.GetTextureOr(tooltipCfg.WhisperBoxPath, NanotrasenStylesheet.TextureRoot).IntoPatch(StyleBox.Margin.All, 2);
        whisperBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

        return
        [
            E<Tooltip>()
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                .Prop(Tooltip.StylePropertyPanel, tooltipBox),
            E<PanelContainer>()
                .Class(ExamineSystem.StyleClassEntityTooltip)
                .Panel(tooltipBox),
            E<PanelContainer>()
                .Class("speechBox", "sayBox")
                .Panel(tooltipBox),
            E<PanelContainer>()
                .Class("speechBox", "whisperBox")
                .Panel(whisperBox),

            E<PanelContainer>()
                .Class("speechBox", "whisperBox")
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontStack.FontKind.Italic)),
            E<PanelContainer>()
                .Class("speechBox", "emoteBox")
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontStack.FontKind.Italic)),
        ];
    }
}
