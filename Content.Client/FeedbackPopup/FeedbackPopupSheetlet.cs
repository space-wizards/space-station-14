using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.FeedbackPopup;

[CommonSheetlet]
public sealed class FeedbackPopupSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var borderTop = new StyleBoxFlat()
        {
            BorderColor = sheet.SecondaryPalette.Base,
            BorderThickness = new Thickness(0, 1, 0, 0),
        };

        var borderBottom = new StyleBoxFlat()
        {
            BorderColor = sheet.SecondaryPalette.Base,
            BorderThickness = new Thickness(0, 0, 0, 1),
        };

        return
        [
            E<PanelContainer>()
                .Identifier("FeedbackBorderThinTop")
                .Prop(PanelContainer.StylePropertyPanel, borderTop),
            E<PanelContainer>()
                .Identifier("FeedbackBorderThinBottom")
                .Prop(PanelContainer.StylePropertyPanel, borderBottom),
        ];
    }
}
