using Content.Client.Stylesheets.Palette;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.StylesheetFactories;

/// <summary>
/// StylesheetFactory that produces the stylesheet used for OOC UIs like admin/debug UIs.
/// </summary>
[Virtual]
public class SystemStylesheetFactory : CommonStylesheetFactory
{
    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        {
            typeof(TextureResource), [
                new ResPath("/Textures/Interface/System"),
                // Fallback to nano if it can't be found in System
                new ResPath("/Textures/Interface/Nano"),
                new ResPath("/Textures/Interface"),
            ]
        },
    };

    public override ColorPalette PrimaryPalette => Palettes.Cyan;
    public override ColorPalette SecondaryPalette => Palettes.Neutral;
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => Palettes.Maroon;
}
