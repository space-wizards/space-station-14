using System.Numerics;

namespace Content.Shared.Humanoid;

public static partial class ColorHelper
{
    /// <summary>
    /// Converts a Linear sRGB color to an Oklab color.
    /// </summary>
    public static Vector3 LinearSrgbToOklab(Vector3 c)
    {
        float l = 0.4122214708f * c.X + 0.5363325363f * c.Y + 0.0514459929f * c.Z;
        float m = 0.2119034982f * c.X + 0.6806995451f * c.Y + 0.1073969566f * c.Z;
        float s = 0.0883024619f * c.X + 0.2817188376f * c.Y + 0.6299787005f * c.Z;

        float l_ = (float)Math.Cbrt(l);
        float m_ = (float)Math.Cbrt(m);
        float s_ = (float)Math.Cbrt(s);

        return new Vector3(
            0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
            1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
            0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_
        );
    }

    /// <summary>
    /// Converts an Oklab color to a Linear sRGB color.
    /// </summary>
    /// <remarks>
    /// No gamut clipping is done.
    /// </remarks>
    public static Vector3 OklabToLinearSrgb(Vector3 c)
    {
        float l_ = c.X + 0.3963377774f * c.Y + 0.2158037573f * c.Z;
        float m_ = c.X - 0.1055613458f * c.Y - 0.0638541728f * c.Z;
        float s_ = c.X - 0.0894841775f * c.Y - 1.2914855480f * c.Z;

        float l = l_ * l_ * l_;
        float m = m_ * m_ * m_;
        float s = s_ * s_ * s_;

        return new Vector3(
            +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
            -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
            -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s
        );
    }

    /// <summary>
    /// Converts a Lab color to the corresponding LCh color.
    /// </summary>
    public static Vector3 LabToLch(Vector3 c)
    {
        return new Vector3(
            c.X, (float)Math.Sqrt((c.Y * c.Y) + (c.Z * c.Z)), (float)Math.Atan2(c.Z, c.Y)
        );
    }

    /// <summary>
    /// Converts a LCh color to the corresponding Lab color.
    /// </summary>
    public static Vector3 LchToLab(Vector3 c)
    {
        return new Vector3(
            c.X, (float)(c.Y * Math.Cos(c.Z)), (float)(c.Y * Math.Sin(c.Z))
        );
    }

    /// <summary>
    /// Converts a RobustToolbox Color, which is sRGB + Alpha, to Linear sRGB.
    /// Alpha is discarded in the process.
    /// </summary>
    public static Vector3 ToLinearSrgb(Color color)
    {
        return new Vector3(FInv(color.R), FInv(color.G), FInv(color.B));
    }

    /// <summary>
    /// Converts a Linear sRGB color to a RobustToolbox color.
    /// </summary>
    /// <remarks>
    /// No gamut clipping is done.
    /// </remarks>
    public static Color FromLinearSrgb(Vector3 color)
    {
        return new Color(F(color.X), F(color.Y), F(color.Z));
    }

    private static float F(float x)
    {
        if (x >= 0.0031308)
            return (float)(1.055 * Math.Pow(x, 1.0 / 2.4) - 0.055);
        else
            return (float)(12.92 * x);
    }

    private static float FInv(float x)
    {
        if (x >= 0.04045)
            return (float)Math.Pow((x + 0.055) / (1 + 0.055), 2.4);
        else
            return (float)(x / 12.92);
    }
}
