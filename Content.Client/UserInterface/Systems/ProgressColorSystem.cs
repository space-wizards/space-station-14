using System.Linq;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Systems;

/// <summary>
/// This system handles getting an interpolated color based on the value of a cvar.
/// </summary>
public sealed class ProgressColorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private static bool _colorBlindFriendly;

    private static IReadOnlyList<Color> _viridis = new[]
    {
        new Color(253, 221, 37),
        new Color(94, 201, 98),
        new Color(33, 145, 140),
        new Color(59, 82, 139),
        new Color(68, 1, 84)
    };

    /// <inheritdoc/>
    public override void Initialize()
    {
        Subs.CVar(_configuration, CCVars.ColorblindFriendly, OnColorBlindFriendlyChanged, true);
    }

    private void OnColorBlindFriendlyChanged(bool newvalue, in CVarChangeInfo info)
    {
        _colorBlindFriendly = newvalue;
    }

    public static Color GetProgressColor(float progress)
    {
        if (!_colorBlindFriendly)
        {
            if (progress >= 1.0f)
            {
                return new Color(0f, 1f, 0f);
            }

            // lerp
            var hue = (5f / 18f) * progress;
            return Color.FromHsv((hue, 1f, 0.75f, 1f));
        }

        return InterpolateColorGaussian(_viridis.ToArray(), progress);
    }

    /// <summary>
    /// Interpolates between multiple colors based on a gaussian distribution.
    /// </summary>
    public static Color InterpolateColorGaussian(Color[] colors, double x)
    {
        double r = 0.0, g = 0.0, b = 0.0;
        var total = 0.0;
        var step = 1.0 / (colors.Length - 1);
        var mu = 0.0;
        const double sigma2 = 0.035;

        foreach (var _ in colors)
        {
            total += Math.Exp(-(x - mu) * (x - mu) / (2.0 * sigma2)) / Math.Sqrt(2.0 * Math.PI * sigma2);
            mu += step;
        }

        mu = 0.0;
        foreach(var color in colors)
        {
            var percent = Math.Exp(-(x - mu) * (x - mu) / (2.0 * sigma2)) / Math.Sqrt(2.0 * Math.PI * sigma2);
            mu += step;

            r += color.R * percent / total;
            g += color.G * percent / total;
            b += color.B * percent / total;
        }

        return new Color((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}
