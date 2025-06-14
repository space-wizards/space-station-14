using System.Security.Cryptography;
using Microsoft.VisualBasic.CompilerServices;

namespace Content.Shared.Humanoid;

public static class SkinColor
{
    public const float MaxTintedHuesSaturation = 0.1f;
    public const float MinTintedHuesLightness = 0.85f;

    public const float MinHuesLightness = 0.175f;

    public const float MinFeathersHue = 29f / 360;
    public const float MaxFeathersHue = 174f / 360;
    public const float MinFeathersSaturation = 20f / 100;
    public const float MaxFeathersSaturation = 88f / 100;
    public const float MinFeathersValue = 36f / 100;
    public const float MaxFeathersValue = 55f / 100;

    public static Color ValidHumanSkinTone => Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

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

    /// <summary>
    ///     Converts a Color proportionally to the allowed vox color range.
    ///     Will NOT preserve the specific input color even if it is within the allowed vox color range.
    /// </summary>
    /// <param name="color">Color to convert</param>
    /// <returns>Vox feather coloration</returns>
    public static Color ProportionalVoxColor(Color color)
    {
        var newColor = Color.ToHsv(color);

        newColor.X = newColor.X * (MaxFeathersHue - MinFeathersHue) + MinFeathersHue;
        newColor.Y = newColor.Y * (MaxFeathersSaturation - MinFeathersSaturation) + MinFeathersSaturation;
        newColor.Z = newColor.Z * (MaxFeathersValue - MinFeathersValue) + MinFeathersValue;

        return Color.FromHsv(newColor);
    }

    // /// <summary>
    // ///      Ensures the input Color is within the allowed vox color range.
    // /// </summary>
    // /// <param name="color">Color to convert</param>
    // /// <returns>The same Color if it was within the allowed range, or the closest matching Color otherwise</returns>
    public static Color ClosestVoxColor(Color color)
    {
        var hsv = Color.ToHsv(color);

        hsv.X = Math.Clamp(hsv.X, MinFeathersHue, MaxFeathersHue);
        hsv.Y = Math.Clamp(hsv.Y, MinFeathersSaturation, MaxFeathersSaturation);
        hsv.Z = Math.Clamp(hsv.Z, MinFeathersValue, MaxFeathersValue);

        return Color.FromHsv(hsv);
    }

    /// <summary>
    ///     Verify if this color is a valid vox feather coloration, or not.
    /// </summary>
    /// <param name="color">The color to verify</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool VerifyVoxFeathers(Color color)
    {
        var colorHsv = Color.ToHsv(color);

        if (colorHsv.X < MinFeathersHue || colorHsv.X > MaxFeathersHue)
            return false;

        if (colorHsv.Y < MinFeathersSaturation || colorHsv.Y > MaxFeathersSaturation)
            return false;

        if (colorHsv.Z < MinFeathersValue || colorHsv.Z > MaxFeathersValue)
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
            HumanoidSkinColor.VoxFeathers => ClosestVoxColor(color),
            _ => color
        };
    }
}

public enum HumanoidSkinColor : byte
{
    HumanToned,
    Hues,
    VoxFeathers, // Vox feathers are limited to a specific color range
    TintedHues, //This gives a color tint to a humanoid's skin (10% saturation with full hue range).
}
