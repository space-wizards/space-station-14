using Content.Client.Stylesheets.Palette;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.StylesheetFactories;

/// <summary>
/// StylesheetFactory that produces the stylesheet used for IC-related UIs like most of the game's user interfaces.
/// </summary>
[Virtual]
public class NanotrasenStylesheetFactory : CommonStylesheetFactory
{
    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        {
            typeof(TextureResource), [
                new ResPath("/Textures/Interface/Nano"),
                new ResPath("/Textures/Interface"),
            ]
        },
    };

    public override ColorPalette PrimaryPalette => Palettes.Navy;
    public override ColorPalette SecondaryPalette => Palettes.Slate;
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => Palettes.Gold;
}
