using System.Linq;
using Robust.Client.Graphics;

namespace Content.Client.Stylesheets.Fonts;

/// <summary>
/// Helper functions for selecting fonts.
/// </summary>
public static class FontSelectionHelpers
{
    /// <summary>
    /// Select the system font that matches the desired properties as closely as possible.
    /// </summary>
    public static ISystemFontFace SelectClosest(
        ISystemFontFace[] options,
        FontWeight weight = FontWeight.Normal,
        FontSlant slant = FontSlant.Normal,
        FontWidth width = FontWidth.Normal)
    {
        return options.OrderBy(x => Math.Abs((int)x.Weight - (int)weight))
            .ThenBy(x => Math.Abs((int)x.Slant - (int)slant))
            .ThenBy(x => Math.Abs((int)x.Width - (int)width))
            .First();
    }
}
