using Content.Client.Stylesheets.Redux.Colorspace;
using Content.Client.Stylesheets.Redux.NTSheetlets;
using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.Stylesheets;

public sealed partial class NanotrasenStylesheet : IButtonConfig, IPanelPalette
{
    /*
     * NT Colors.
     * Do NOT copy-paste these. I will find you.
     * Seriously, use the stylesheet.
     */

    private static readonly Color PrimaryColor = Color.FromHex("#575B7F");
    private static readonly Color SecondaryColor = Color.FromHex("#5B5D6C");
    private static readonly Color PositiveColor = Color.FromHex("#3E6C45");
    private static readonly Color NegativeColor = Color.FromHex("#CF2F2F");
    private static readonly Color HighlightColor = Color.FromHex("#A88B5E");

    // The primary/vibrant palette used by interactables like buttons.
    public override Color[] PrimaryPalette { get; } =  new[]
    {
        PrimaryColor,
        PrimaryColor.NudgeLightness(-7.5f),
        PrimaryColor.NudgeLightness(-7.5f * 2),
        PrimaryColor.NudgeLightness(-7.5f * 3),
        PrimaryColor.NudgeLightness(-7.5f * 4),
    };

    // The secondary/mundane palette used by background elements.
    public override Color[] SecondaryPalette { get; } = new[]
    {
        SecondaryColor,
        SecondaryColor.NudgeLightness(-7.5f),
        SecondaryColor.NudgeLightness(-7.5f * 2),
        SecondaryColor.NudgeLightness(-7.5f * 3),
        SecondaryColor.NudgeLightness(-7.5f * 4),
    };

    // A (traditionally) green palette used for positive actions.
    public override Color[] PositivePalette { get; } = new[]
    {
        PositiveColor,
        PositiveColor.NudgeLightness(-7.5f),
        PositiveColor.NudgeLightness(-7.5f * 2),
        PositiveColor.NudgeLightness(-7.5f * 3),
        PositiveColor.NudgeLightness(-7.5f * 4),
    };

    // A (traditionally) red palette used for negative actions.
    public override Color[] NegativePalette { get; } = new[]
    {
        NegativeColor,
        NegativeColor.NudgeLightness(-7.5f),
        NegativeColor.NudgeLightness(-7.5f * 2),
        NegativeColor.NudgeLightness(-7.5f * 3),
        NegativeColor.NudgeLightness(-7.5f * 4),
    };


    public override Color[] HighlightPalette { get; } = new[]
    {
        HighlightColor,
        HighlightColor.NudgeLightness(-7.5f),
        HighlightColor.NudgeLightness(-7.5f * 2),
        HighlightColor.NudgeLightness(-7.5f * 3),
        HighlightColor.NudgeLightness(-7.5f * 4),
    };


    Color IPanelPalette.BackingPanelPalette => SecondaryPalette[3];
}
