using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Systems;

/// <summary>
/// This system handles getting an interpolated color based on the value of a cvar.
/// </summary>
public sealed class ProgressColorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private bool _colorBlindFriendly;

    private static readonly Color[] Plasma =
    {
        new(240, 249, 33),
        new(248, 149, 64),
        new(204, 71, 120),
        new(126, 3, 168),
        new(13, 8, 135)
    };

    /// <inheritdoc/>
    public override void Initialize()
    {
        Subs.CVar(_configuration, CCVars.AccessibilityColorblindFriendly, OnColorBlindFriendlyChanged, true);
    }

    private void OnColorBlindFriendlyChanged(bool value, in CVarChangeInfo info)
    {
        _colorBlindFriendly = value;
    }

    public Color GetProgressColor(float progress)
    {
        if (!_colorBlindFriendly)
        {
            if (progress >= 1.0f)
            {
                return new Color(0f, 1f, 0f);
            }

            // lerp
            var hue = 5f / 18f * progress;
            return Color.FromHsv((hue, 1f, 0.75f, 1f));
        }

        return InterpolateColorGaussian(Plasma, progress);
    }

    /// <summary>
    /// Interpolates between multiple colors based on a gaussian distribution.
    /// Taken from https://stackoverflow.com/a/26103117
    /// </summary>
    public static Color InterpolateColorGaussian(Color[] colors, double x)
    {
        double r = 0.0, g = 0.0, b = 0.0;
        var total = 0f;
        var step = 1.0 / (colors.Length - 1);
        var mu = 0.0;
        const double sigma2 = 0.035;

        foreach(var color in colors)
        {
            var percent = Math.Exp(-(x - mu) * (x - mu) / (2.0 * sigma2)) / Math.Sqrt(2.0 * Math.PI * sigma2);
            total += (float) percent;
            mu += step;

            r += color.R * percent;
            g += color.G * percent;
            b += color.B * percent;
        }

        return new Color((float) r / total, (float) g / total, (float) b / total);
    }
}
