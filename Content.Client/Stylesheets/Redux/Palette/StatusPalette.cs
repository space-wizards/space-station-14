using Content.Client.Stylesheets.Redux.Colorspace;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.Palette;

public sealed class StatusPalette(Color[] statusColors)
{
    /// The status colors to blend between, these are equally spaced from 0 to 1 with 0 being StatusColors[0], and 1 being
    /// StatusColors.Last()
    ///
    /// Traditionally, something like { Red, Yellow, Green }
    public Color[] StatusColors = statusColors;

    /// <param name="factor">The severity of this status from 0 to 1. Traditionally, 1 will be green and 0 red</param>
    /// <returns>The color for this status, linearly Oklab blended between equal intervals of the colors in StatusColor</returns>
    public Color GetStatusColor(float factor)
    {
        DebugTools.Assert(factor >= 0.0 && factor <= 1.0);
        DebugTools.Assert(StatusColors.Length >= 1);

        if (StatusColors.Length == 1)
            return StatusColors[0];

        var intervals = StatusColors.Length - 1;

        var fromIdx = (int) Math.Floor(factor * intervals);
        if (factor is 1.0f or 0.0f)
            return StatusColors[fromIdx];

        var from = StatusColors[fromIdx];
        var to = StatusColors[fromIdx + 1];
        var f = (factor - (float) fromIdx / intervals) * intervals;

        return (Color) OklabColor.Blend(new OklabColor(from), new OklabColor(to), f);
    }
}
