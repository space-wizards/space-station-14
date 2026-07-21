using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetDefinition))]
public sealed class PanelSheetlet<T> : ISheetlet<T>
    where T : IButtonConfig, IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
    {
        var boxLight = new StyleBoxFlat()
        {
            BackgroundColor = config.SecondaryPalette.BackgroundLight,
        };
        var boxDark = new StyleBoxFlat()
        {
            BackgroundColor = config.SecondaryPalette.BackgroundDark,
        };
        var boxInsetDark = new StyleBoxFlat()
        {
            BackgroundColor = config.SecondaryPalette.BackgroundDark,
            BorderColor = config.PrimaryPalette.Background,
            BorderThickness = new Thickness(2f),
        };

        var boxPositive = new StyleBoxFlat { BackgroundColor = config.PositivePalette.Background };
        var boxNegative = new StyleBoxFlat { BackgroundColor = config.NegativePalette.Background };
        var boxHighlight = new StyleBoxFlat { BackgroundColor = config.HighlightPalette.Background };
        var boxDropTarget = new StyleBoxFlat
        {
            BackgroundColor = config.ButtonPalette.BackgroundDark.WithAlpha(0.5f),
            BorderColor = config.ButtonPalette.Base,
            BorderThickness = new(2)
        };

        return
        [
            E<PanelContainer>().Class(StyleClass.PanelLight).Panel(boxLight),
            E<PanelContainer>().Class(StyleClass.PanelDark).Panel(boxDark),
            E<PanelContainer>().Class(StyleClass.PanelDropTarget).Panel(boxDropTarget),
            E<PanelContainer>().Class(StyleClass.PanelInsetDark).Panel(boxInsetDark),

            E<PanelContainer>().Class(StyleClass.Positive).Panel(boxPositive),
            E<PanelContainer>().Class(StyleClass.Negative).Panel(boxNegative),
            E<PanelContainer>().Class(StyleClass.Highlight).Panel(boxHighlight),

            // TODO: this should probably be cleaned up but too many UIs rely on this hardcoded color so I'm scared to touch it
            E<PanelContainer>()
                .Class("BackgroundDark")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(Color.FromHex("#25252A"))),

            // panels that have the same corner bezels as buttons
            E()
                .Class(StyleClass.BackgroundPanel)
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.BaseStyleBox(sheet, config))
                .Modulate(config.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelDark)
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.BaseStyleBox(sheet, config))
                .Modulate(config.SecondaryPalette.BackgroundDark),
            E()
                .Class(StyleClass.BackgroundPanelOpenLeft)
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.OpenLeftStyleBox(sheet, config))
                .Modulate(config.SecondaryPalette.Background),
            E()
                .Class(StyleClass.BackgroundPanelOpenRight)
                .Prop(PanelContainer.StylePropertyPanel, StyleBoxHelpers.OpenRightStyleBox(sheet, config))
                .Modulate(config.SecondaryPalette.Background),
        ];
    }
}
