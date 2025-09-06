using System.Numerics;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid.ColoringScheme;

public sealed partial class HumanToned : ColoringSchemeRule
{
    public override Color Randomize(IRobustRandom random)
    {
        var tone = random.Next(0, 101);
        return ToneToColor(tone);
    }

    public override bool Verify(Color color)
    {
        var hsv = Color.ToHsv(color);
        float hue = hsv.X * 360f;
        float sat = hsv.Y * 100f;
        float val = hsv.Z * 100f;

        return hue >= 25 && hue <= 45 && sat >= 20 && val >= 20;
    }

    public override Color Clamp(Color color)
    {
        float tone = ColorToTone(color);
        return ToneToColor((int)Math.Clamp(tone, 0, 100));
    }

    private static Color ToneToColor(int tone)
    {
        // 0 - 100, 0 being gold/yellowish and 100 being dark
        // HSV based
        //
        // 0 - 20 changes the hue
        // 20 - 100 changes the value
        // 0 is 45 - 20 - 100
        // 20 is 25 - 20 - 100
        // 100 is 25 - 100 - 20

        tone = Math.Clamp(tone, 0, 100);
        float rangeOffset = tone - 20;

        float hue = 25f;
        float sat = 20f;
        float val = 100f;

        if (rangeOffset <= 0)
        {
            hue += Math.Abs(rangeOffset);
        }
        else
        {
            sat += rangeOffset;
            val -= rangeOffset;
        }

        return Color.FromHsv(new Vector4(hue / 360f, sat / 100f, val / 100f, 1f));
    }

    private static float ColorToTone(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (Math.Clamp(hsv.X, 25f / 360f, 1f) > 25f / 360f && hsv.Z == 1f)
            return Math.Abs(45 - (hsv.X * 360f));

        return hsv.Y * 100f;
    }
}
