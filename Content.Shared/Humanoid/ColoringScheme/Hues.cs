using System.Numerics;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid.ColoringScheme;

public sealed partial class Hues : ColoringSchemeRule
{
    private const float MinLightness = 0.175f;

    public override Color Randomize(IRobustRandom random)
    {
        var hsv = new Vector4(random.NextFloat(1f), random.NextFloat(1f), random.NextFloat(1f) * (1f - MinLightness) + MinLightness, 1f);
        return Color.FromHsv(hsv);
    }

    public override bool Verify(Color color)
    {
        var hsv = Color.ToHsv(color);
        return hsv.Z >= MinLightness;
    }

    public override Color Clamp(Color color)
    {
        var hsv = Color.ToHsv(color);
        hsv.Z = Math.Max(hsv.Z, MinLightness);
        return Color.FromHsv(hsv);
    }
}
