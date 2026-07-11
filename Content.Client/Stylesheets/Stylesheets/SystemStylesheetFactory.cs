using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Stylesheets;

[Virtual]
public class SystemStylesheetFactory : CommonStylesheetFactory, IPaletteConfig
{
    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        {
            typeof(TextureResource), [
                new ResPath("/Textures/Interface/System"),
                // Fallback to nano if it can't be found in System
                new ResPath("/Textures/Interface/Nano"),
                new ResPath("/Textures/Interface")
            ]
        },
    };

    public override ColorPalette PrimaryPalette => Palettes.Cyan;
    public override ColorPalette SecondaryPalette => Palettes.Neutral;
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => Palettes.Maroon;
}
