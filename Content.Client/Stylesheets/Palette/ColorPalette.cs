using Content.Client.Stylesheets.Colorspace;

// ReSharper disable MemberCanBePrivate.Global

namespace Content.Client.Stylesheets.Palette;

/// <remarks>
///     Don't be afraid to add a lot of fields here! This class is made for readability.
/// </remarks>
public sealed class ColorPalette
{
    /// <summary>
    ///     The root color all others are derived from
    /// </summary>
    public Color Base;

    public float LightnessShift;
    public float ChromaShift;

    public Color Element;
    public Color HoveredElement;
    public Color PressedElement;
    public Color DisabledElement;

    public Color Background;
    public Color BackgroundLight;
    public Color BackgroundDark;

    public Color Text;
    public Color TextDark;

    /// <summary>
    ///     Given the initialized configuration properties, this constructor sets all the color properties to
    ///     derivations of the <see cref="Base"/> color. You can override specific fields or groups of fields with
    ///     their own values by passing them in as arguments.
    /// </summary>
    /// <remarks>
    ///     This constructor should be used with named arguments to override specific arguments in the method
    ///     invocation. Do NOT use positional arguments, AKA list them out one by one. I don't think I have to
    ///     justify why.
    /// </remarks>
    public ColorPalette(
        string hex,
        float lightnessShift = 0.06f,
        float chromaShift = 0.00f,
        Color? element = null,
        Color? hoveredElement = null,
        Color? pressedElement = null,
        Color? disabledElement = null,
        Color? background = null,
        Color? backgroundLight = null,
        Color? backgroundDark = null,
        Color? text = null,
        Color? textDark = null
    )
    {
        Base = Color.FromHex(hex);

        LightnessShift = lightnessShift;
        ChromaShift = chromaShift;

        Element = element ?? Shift(Base, -1); //                        Shift(Base, -1)
        HoveredElement = hoveredElement ?? Shift(Element, 1); //        Shift(Base,  0)
        PressedElement = pressedElement ?? Shift(Element, -1); //       Shift(Base, -2)
        DisabledElement = disabledElement ?? Shift(Element, -2) //      Shift(Base, -3)
            .NudgeChroma(-ChromaShift * 2);

        Background = background ?? Shift(Base, -3); //                  Shift(Base, -3)
        BackgroundLight = backgroundLight ?? Shift(Background, 1); //   Shift(Base, -2)
        BackgroundDark = backgroundDark ?? Shift(Background, -1); //    Shift(Base, -4)

        Text = text ?? Base; //                                         Shift(Base,  0)
        TextDark = textDark ?? Shift(Text, -1); //                      Shift(Base, -1)
    }

    private Color Shift(Color from, float factor)
    {
        return from.NudgeLightness(LightnessShift * factor).NudgeChroma(ChromaShift * factor);
    }
}
