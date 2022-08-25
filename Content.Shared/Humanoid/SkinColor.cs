namespace Content.Shared.Humanoid;

public static class SkinColor
{
    public static Color ValidHumanSkinTone => Color.FromHsv(new Vector4(0.25f, 0.2f, 1f, 1f));

    public static Color ValidTintedHuesSkinTone(Color color)
    {
        var hsv = Color.ToHsv(color);
        hsv.Y = .1f;

        return Color.FromHsv(hsv);
    }

    public static Color HumanSkinTone(int tone)
    {
        if (tone < 0 || tone > 100)
        {
            throw new ArgumentException("Skin tone value was under 0 or over 100.");
        }

        var rangeOffset = tone - 20;

        float hue = 25;
        float sat = 20;
        float val = 100;

        if (rangeOffset <= 0)
        {
            hue += Math.Abs(rangeOffset);
        }
        else
        {
            sat += rangeOffset;
            val -= rangeOffset;
        }

        var color = Color.FromHsv(new Vector4(hue / 360, sat / 100, val / 100, 1.0f));

        return color;
    }

    public static float HumanSkinToneFromColor(Color color)
    {
        var hsv = Color.ToHsv(color);
        // check for hue/value first, if hue is lower than this percentage
        // and value is 1.0
        // then it'll be hue
        if (Math.Clamp(hsv.X, 25f / 360f, 1) > 25f / 360f
            && hsv.Z == 1.0)
        {
            return Math.Abs(45 - (hsv.X * 360));
        }
        // otherwise it'll directly be the saturation
        else
        {
            return hsv.Y * 100;
        }
    }

    public static bool VerifyHumanSkinTone(Color color)
    {
        var colorValues = Color.ToHsv(color);

        var hue = colorValues.X * 360f;
        var sat = colorValues.Y * 100f;
        var val = colorValues.Z * 100f;
        // rangeOffset makes it so that this value
        // is 25 <= hue <= 45
        if (hue < 25 || hue > 45)
        {
            return false;
        }

        // rangeOffset makes it so that these two values
        // are 20 <= sat <= 100 and 20 <= val <= 100
        // where saturation increases to 100 and value decreases to 20
        if (sat < 20 || val < 20)
        {
            return false;
        }

        return true;
    }

    public static Color TintedHues(Color color)
    {
        var newColor = Color.ToHsv(color);
        newColor.Y = .1f;

        return Color.FromHsv(newColor);
    }

    public static bool VerifyTintedHues(Color color)
    {
        // tinted hues just ensures saturation is always .1, or 10% saturation at all times
        return Color.ToHsv(color).Y != .1f;
    }
}
