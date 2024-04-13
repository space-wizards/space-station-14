using System.Security.Cryptography;
using Microsoft.VisualBasic.CompilerServices;

namespace Content.Shared.Humanoid;

public static class SkinColor
{
    public const float MaxTintedHuesSaturation = 0.1f;
    public const float MinTintedHuesLightness = 0.85f;

    public const float MinHuesLightness = 0.175f;

    public static Color ValidHumanSkinTone => Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));
    public static Color ValidVoxFeathers => Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    /// <summary>
    ///     Turn a color into a valid tinted hue skin tone.
    /// </summary>
    /// <param name="color">The color to validate</param>
    /// <returns>Validated tinted hue skin tone</returns>
    public static Color ValidTintedHuesSkinTone(Color color)
    {
        return TintedHues(color);
    }

    /// <summary>
    ///     Get a human skin tone based on a scale of 0 to 100. The value is clamped between 0 and 100.
    /// </summary>
    /// <param name="tone">Skin tone. Valid range is 0 to 100, inclusive. 0 is gold/yellowish, 100 is dark brown.</param>
    /// <returns>A human skin tone.</returns>
    public static Color HumanSkinTone(int tone)
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

    /// <summary>
    ///     Gets a human skin tone from a given color.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    /// <remarks>
    ///     Does not cause an exception if the color is not originally from the human color range.
    ///     Instead, it will return the approximation of the skin tone value.
    /// </remarks>
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

    /// <summary>
    ///     Verify if a color is in the human skin tone range.
    /// </summary>
    /// <param name="color">The color to verify</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool VerifyHumanSkinTone(Color color)
    {
        var colorValues = Color.ToHsv(color);

        var hue = Math.Round(colorValues.X * 360f);
        var sat = Math.Round(colorValues.Y * 100f);
        var val = Math.Round(colorValues.Z * 100f);
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

    /// <summary>
    ///     Convert a color to the 'tinted hues' skin tone type.
    /// </summary>
    /// <param name="color">Color to convert</param>
    /// <returns>Tinted hue color</returns>
    public static Color TintedHues(Color color)
    {
        var newColor = Color.ToHsl(color);
        newColor.Y *= MaxTintedHuesSaturation;
        newColor.Z = MathHelper.Lerp(MinTintedHuesLightness, 1f, newColor.Z);

        return Color.FromHsv(newColor);
    }

    /// <summary>
    ///     Verify if this color is a valid tinted hue color type, or not.
    /// </summary>
    /// <param name="color">The color to verify</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool VerifyTintedHues(Color color)
    {
        // tinted hues just ensures saturation is always .1, or 10% saturation at all times
        return Color.ToHsl(color).Y <= MaxTintedHuesSaturation && Color.ToHsl(color).Z >= MinTintedHuesLightness;
    }





    //TODO: comments and whatever
    public static Color VoxFeathers(int tone)
    {
        tone = Math.Clamp(tone, 0, 99);

        // The range is split into 5 equal sections with one Hue each
        // Position within each section sets Saturation proportionally
        // V-value is constant

        var d1 = tone / 10;
        var d2 = tone % 10;

        //var hueMin = 29f;
        //var hueMax = 174f;
        //ar satMin = 20f;
        //var satMax = 88f;
        //var hue = (hueMax - hueMin) / 9 * d1 + hueMin ;

        var hue = 85f;
        // TODO remove the previous calculation in the last line
        switch (d1)
        {
            case var i when i <= 1:
                hue = 29;
                break;

            case var i when i <= 3:
                hue = 66;
                break;

            case var i when i <= 5:
                hue = 85;
                break;

            case var i when i <= 7:
                hue = 148;
                break;

            case var i when i <= 9:
                hue = 174;
                break;
        }

        var leftover = d1 % 2f;
        var sat = (d2 + leftover * 10) *5;
        //var sat = (d2 + leftover * 10) *1.47f + 20;


        //var sat = (satMax - satMin) / 9 * d2 + satMin;
        var val = 44.75f ;

        var color = Color.FromHsv(new Vector4(hue / 360, sat / 100, val / 100, 1.0f));

        return color;
    }

    public static float VoxFeathersFromColor(Color color)
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

    //TODO: comments and whatever
    public static bool VerifyVoxFeathers(Color color)
    {
        var colorValues = Color.ToHsv(color);

        var hue = Math.Round(colorValues.X * 360f);
        var sat = Math.Round(colorValues.Y * 100f);
        var val = Math.Round(colorValues.Z * 100f);

        //TODO make this less ugly
        var h1 = 29f;
        var h2 = 66f;
        var h3 = 85f;
        var h4 = 148f;
        var h5 = 174f;

        if (Math.Abs(hue - h1) > 1f && Math.Abs(hue - h2) > 1f  && Math.Abs(hue - h3) > 1f  && Math.Abs(hue - h4) > 1f  && Math.Abs(hue - h5) > 1f )
            return false;

        if (val < 44f || val > 45f)
            return false;

        return true;
    }





    /// <summary>
    ///     This takes in a color, and returns a color guaranteed to be above MinHuesLightness
    /// </summary>
    /// <param name="color"></param>
    /// <returns>Either the color as-is if it's above MinHuesLightness, or the color with luminosity increased above MinHuesLightness</returns>
    public static Color MakeHueValid(Color color)
    {
        var manipulatedColor = Color.ToHsv(color);
        manipulatedColor.Z = Math.Max(manipulatedColor.Z, MinHuesLightness);
        return Color.FromHsv(manipulatedColor);
    }

    /// <summary>
    ///     Verify if this color is above a minimum luminosity
    /// </summary>
    /// <param name="color"></param>
    /// <returns>True if valid, false if not</returns>
    public static bool VerifyHues(Color color)
    {
        return Color.ToHsv(color).Z >= MinHuesLightness;
    }

    public static bool VerifySkinColor(HumanoidSkinColor type, Color color)
    {
        return type switch
        {
            HumanoidSkinColor.HumanToned => VerifyHumanSkinTone(color),
            HumanoidSkinColor.TintedHues => VerifyTintedHues(color),
            HumanoidSkinColor.Hues => VerifyHues(color),
            HumanoidSkinColor.VoxFeathers => VerifyVoxFeathers(color),
            _ => false,
        };
    }

    public static Color ValidSkinTone(HumanoidSkinColor type, Color color)
    {
        return type switch
        {
            HumanoidSkinColor.HumanToned => ValidHumanSkinTone,
            HumanoidSkinColor.TintedHues => ValidTintedHuesSkinTone(color),
            HumanoidSkinColor.Hues => MakeHueValid(color),
            HumanoidSkinColor.VoxFeathers => ValidVoxFeathers,
            _ => color
        };
    }
}

public enum HumanoidSkinColor : byte
{
    HumanToned,
    Hues,
    VoxFeathers, // Vox are denied the full rainbow range
    TintedHues, //This gives a color tint to a humanoid's skin (10% saturation with full hue range).
}
