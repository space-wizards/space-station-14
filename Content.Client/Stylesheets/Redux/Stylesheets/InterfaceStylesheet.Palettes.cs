using Content.Client.Stylesheets.Redux.Colorspace;
using Content.Client.Stylesheets.Redux.NTSheetlets;
using Content.Client.Stylesheets.Redux.SheetletConfig;

namespace Content.Client.Stylesheets.Redux.Stylesheets;

public partial class InterfaceStylesheet : IPanelPalette
{
    private static readonly Color PrimaryColor = Color.FromHex("#4a6173");
    private static readonly Color SecondaryColor = Color.FromHex("#5e5e5e");
    private static readonly Color PositiveColor = Color.FromHex("#3e6c45");
    private static readonly Color NegativeColor = Color.FromHex("#cf2f2f");
    private static readonly Color HighlightColor = Color.FromHex("#a88b5e");

    // The primary/vibrant palette used by interactables like buttons.
    public override Color[] PrimaryPalette { get; } = new[]
    {
        PrimaryColor,
        PrimaryColor.NudgeLightness(-6f),
        PrimaryColor.NudgeLightness(-12f),
        PrimaryColor.NudgeLightness(-18f),
        PrimaryColor.NudgeLightness(-24f),
    };

    // The secondary/mundane palette used by background elements.
    public override Color[] SecondaryPalette { get; } = new[]
    {
        SecondaryColor,
        SecondaryColor.NudgeLightness(-6f),
        SecondaryColor.NudgeLightness(-12f),
        SecondaryColor.NudgeLightness(-18f),
        SecondaryColor.NudgeLightness(-24f),
    };

    // A (traditionally) green palette used for positive actions.
    public override Color[] PositivePalette { get; } = new[]
    {
        PositiveColor,
        PositiveColor.NudgeLightness(-6f),
        PositiveColor.NudgeLightness(-12f),
        PositiveColor.NudgeLightness(-18f),
        PositiveColor.NudgeLightness(-24f),
    };

    // A (traditionally) red palette used for negative actions.
    public override Color[] NegativePalette { get; } = new[]
    {
        NegativeColor,
        NegativeColor.NudgeLightness(-6f),
        NegativeColor.NudgeLightness(-12f),
        NegativeColor.NudgeLightness(-18f),
        NegativeColor.NudgeLightness(-24f),
    };


    public override Color[] HighlightPalette { get; } = new[]
    {
        HighlightColor,
        HighlightColor.NudgeLightness(-6f),
        HighlightColor.NudgeLightness(-12f),
        HighlightColor.NudgeLightness(-18f),
        HighlightColor.NudgeLightness(-24f),
    };

    Color IPanelPalette.PanelLightColor => SecondaryPalette[2];
    Color IPanelPalette.PanelColor => SecondaryPalette[3];
    Color IPanelPalette.PanelDarkColor => SecondaryPalette[4];
}
