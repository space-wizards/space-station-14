using Content.Client.Stylesheets.Redux.Fonts;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.InterfaceSheetlets;

public sealed class InterfaceTooltipSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var tooltipTexture = sheet.GetTexture("tooltip.png");
        var tooltipBox = new StyleBoxTexture
        {
            Texture = tooltipTexture,
        };
        tooltipBox.SetPatchMargin(StyleBox.Margin.All, 2);
        tooltipBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

        return
        [
            E<PanelContainer>()
                .Class(StyleClass.TooltipPanel)
                .Modulate(Color.Gray.WithAlpha(0.9f))
                .Panel(tooltipBox),
            E<RichTextLabel>()
                .Class(StyleClass.TooltipTitle)
                .Font(sheet.BaseFont.GetFont(14, FontStack.FontKind.Bold)),
            E<RichTextLabel>()
                .Class(StyleClass.TooltipDesc)
                .Font(sheet.BaseFont.GetFont(12)),
        ];
    }
}
