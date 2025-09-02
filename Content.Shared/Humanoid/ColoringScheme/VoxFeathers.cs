using System.Numerics;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid.ColoringScheme;

public sealed partial class VoxFeathers : ColoringSchemeRule
{
    private const float MinHue = 29f / 360f;
    private const float MaxHue = 174f / 360f;
    private const float MinSaturation = 0.2f;
    private const float MaxSaturation = 0.88f;
    private const float MinValue = 0.36f;
    private const float MaxValue = 0.55f;

    public override Color Randomize(IRobustRandom random)
    {
        var h = random.NextFloat() * (MaxHue - MinHue) + MinHue;
        var s = random.NextFloat() * (MaxSaturation - MinSaturation) + MinSaturation;
        var v = random.NextFloat() * (MaxValue - MinValue) + MinValue;
        return Color.FromHsv(new Vector4(h, s, v, 1f));
    }

    public override bool Verify(Color color)
    {
        var hsv = Color.ToHsv(color);
        return hsv.X >= MinHue && hsv.X <= MaxHue &&
               hsv.Y >= MinSaturation && hsv.Y <= MaxSaturation &&
               hsv.Z >= MinValue && hsv.Z <= MaxValue;
    }

    public override Color Clamp(Color color)
    {
        var hsv = Color.ToHsv(color);
        hsv.X = Math.Clamp(hsv.X, MinHue, MaxHue);
        hsv.Y = Math.Clamp(hsv.Y, MinSaturation, MaxSaturation);
        hsv.Z = Math.Clamp(hsv.Z, MinValue, MaxValue);
        return Color.FromHsv(hsv);
    }
}
