namespace Content.Shared.Humanoid;

public static class EyeColor
{
    public const float ShadekinBrightness = 0.251f;
    public const float MinHuesLightness = 0;

    public static bool VerifyShadekin(Color color)
    {
        var colorHsv = Color.ToHsv(color);

        if (colorHsv.Z > ShadekinBrightness)
            return false;

        return true;
    }

    public static Color MakeShadekinValid(Color color)
    {
        var hsv = Color.ToHsv(color);

        hsv.Z = Math.Clamp(hsv.Z, 0, ShadekinBrightness);

        return Color.FromHsv(hsv);
    }

    public static Color MakeHueValid(Color color)
    {
        var manipulatedColor = Color.ToHsv(color);
        manipulatedColor.Z = Math.Max(manipulatedColor.Z, MinHuesLightness);
        return Color.FromHsv(manipulatedColor);
    }

    public static bool VerifyHues(Color color)
    {
        return Color.ToHsv(color).Z >= MinHuesLightness;
    }

    public static bool VerifyEyeColor(HumanoidEyeColor type, Color color)
    {
        return type switch
        {
            HumanoidEyeColor.Hues => VerifyHues(color),
            HumanoidEyeColor.Shadekin => VerifyShadekin(color),
            _ => false,
        };
    }

    public static Color ValidEyeColor(HumanoidEyeColor type, Color color)
    {
        return type switch
        {
            HumanoidEyeColor.Hues => MakeHueValid(color),
            HumanoidEyeColor.Shadekin => MakeShadekinValid(color),
            _ => color
        };
    }
}

public enum HumanoidEyeColor : byte
{
    Hues,
    Shadekin,
}

[ByRefEvent]
public record struct EyeColorInitEvent();