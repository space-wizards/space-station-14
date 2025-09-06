using System.Numerics;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid.ColoringScheme;

public sealed partial class TintedHues : ColoringSchemeRule
{
    private const float MaxSaturation = 0.1f;
    private const float MinLightness = 0.85f;

    public override Color Randomize(IRobustRandom random)
    {
        var hsv = new Vector4(random.NextFloat(1f), MaxSaturation, random.NextFloat(1f) * (1f - MinLightness) + MinLightness, 1f);
        return Color.FromHsv(hsv);
    }

    public override bool Verify(Color color)
    {
        var hsl = Color.ToHsl(color);
        return hsl.Y <= MaxSaturation && hsl.Z >= MinLightness;
    }

    public override Color Clamp(Color color)
    {
        var hsl = Color.ToHsl(color);
        hsl.Y = Math.Min(hsl.Y, MaxSaturation);
        hsl.Z = Math.Max(hsl.Z, MinLightness);
        return Color.FromHsv(hsl);
    }
}
