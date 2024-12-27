using JetBrains.Annotations;
using Robust.Shared.Utility;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Stylesheets.Colorspace;

/// <summary>
///     Oklab is an alternate color space that more accurately imitates how color actually behaves.
///     Useful if you want to adjust the lightness/saturation of a color without potentially altering hue.
///     https://bottosson.github.io/posts/oklab/
///     Oklch hue and chroma are also provided in <see cref="ColorExtensions"/>
/// </summary>
[PublicAPI]
public struct OklabColor
{
    private Vector4 _color;

    /// <summary>
    ///     Lightness/saturation of the value.
    /// </summary>
    public float L
    {
        get => _color.X;
        set => _color.X = value;
    }

    /// <summary>
    ///     The A value in Oklab.
    /// </summary>
    public float A
    {
        get => _color.Y;
        set => _color.Y = value;
    }

    /// <summary>
    ///     The B value in Oklab.
    /// </summary>
    public float B
    {
        get => _color.Z;
        set => _color.Z = value;
    }

    /// <summary>
    ///     Transparency.
    /// </summary>
    public float Alpha
    {
        get => _color.W;
        set => _color.W = value;
    }

    public OklabColor(Color c)
    {
        var (r, g, b) = Color.FromSrgb(c);

        var l = 0.4122214708d * r + 0.5363325363d * g + 0.0514459929d * b;
        var m = 0.2119034982d * r + 0.6806995451d * g + 0.1073969566d * b;
        var s = 0.0883024619d * r + 0.2817188376d * g + 0.6299787005d * b;

        // ReSharper disable InconsistentNaming
        var l_ = double.Cbrt(l);
        var m_ = double.Cbrt(m);
        var s_ = double.Cbrt(s);
        // ReSharper restore InconsistentNaming

        L = (float)(0.2104542553d * l_ + 0.7936177850d * m_ - 0.0040720468d * s_);
        A = (float)(1.9779984951d * l_ - 2.4285922050d * m_ + 0.4505937099d * s_);
        B = (float)(0.0259040371d * l_ + 0.7827717662d * m_ - 0.8086757660d * s_);
        Alpha = c.A;
    }

    public static explicit operator Color(OklabColor c)
    {
        // ReSharper disable InconsistentNaming
        var l_ = c.L + 0.3963377774d * c.A + 0.2158037573d * c.B;
        var m_ = c.L - 0.1055613458d * c.A - 0.0638541728d * c.B;
        var s_ = c.L - 0.0894841775d * c.A - 1.2914855480d * c.B;
        // ReSharper restore InconsistentNaming

        var l = l_ * l_ * l_;
        var m = m_ * m_ * m_;
        var s = s_ * s_ * s_;

        return Color.ToSrgb(new Color(
            (float)(+4.0767416621d * l - 3.3077115913d * m + 0.2309699292d * s),
            (float)(-1.2684380046d * l + 2.6097574011d * m - 0.3413193965d * s),
            (float)(-0.0041960863d * l - 0.7034186147d * m + 1.7076147010d * s),
            c.Alpha
        ));
    }

    /// <param name="a">The color the blend from</param>
    /// <param name="b">The color to blend to</param>
    /// <param name="factor">The amount to blend to from 0 to 1, with 0 being a and 1 being b</param>
    /// <returns> a color thats a linear Oklab blend of a and b</returns>
    public static OklabColor Blend(OklabColor a, OklabColor b, float factor)
    {
        DebugTools.Assert(factor >= 0.0 && factor <= 1.0, "Expected factor >= 0.0 && factor <= 1.0");
        return new OklabColor
        {
            _color = Vector4.Lerp(a._color, b._color, factor),
        };
    }
}
