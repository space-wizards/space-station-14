using Content.Client.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.FeedbackPopup;

[Sheetlet(typeof(CommonStylesheetDefinition))]
public sealed class FeedbackPopupSheetlet<T> : ISheetlet<T>
    where T : IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
    {
        var borderTop = new StyleBoxFlat()
        {
            BorderColor = config.SecondaryPalette.Base,
            BorderThickness = new Thickness(0, 1, 0, 0),
        };

        var borderBottom = new StyleBoxFlat()
        {
            BorderColor = config.SecondaryPalette.Base,
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
