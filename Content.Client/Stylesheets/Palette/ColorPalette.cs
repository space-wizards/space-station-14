using Content.Client.Stylesheets.Colorspace;

// ReSharper disable MemberCanBePrivate.Global

namespace Content.Client.Stylesheets.Palette;

/// <remarks>
///     Don't be afraid to add a lot of fields here! This class is made for readability.
/// </remarks>
public sealed class ColorPalette(string hex = "#000000")
{
    /// <summary>
    ///     The root color all others are derived from
    /// </summary>
    public Color Base = Color.FromHex(hex);

    public float LightnessShift = 0.06f;
    public float ChromaShift = 0.00f;

    /// <summary>
    ///     Given the initialized configuration properties, this method sets all the color properties to derivations of
    ///     the <see cref="Base"/> color. Any colors previously set will not be overriden.
    /// </summary>
    /// <remarks>
    ///     Detects if a color has been overriden by whether it's the default value (black) or not. Intended use of this
    ///     function is to override specific fields with brace initialization before calling this method.
    /// </remarks>
    public ColorPalette Construct()
    {
        if (Element == default)
            Element = Shift(Base, -1); //               Shift(Base, -1)
        if (HoveredElement == default)
            HoveredElement = Shift(Element, 1); //      Shift(Base,  0)
        if (PressedElement == default)
            PressedElement = Shift(Element, -1); //     Shift(Base, -2)
        if (DisabledElement == default)
        {
            DisabledElement = Shift(Element, -2); //    Shift(Base, -3)
            DisabledElement = DisabledElement.NudgeChroma(-ChromaShift * 2);
        }

        if (Background == default)
            Background = Shift(Base, -3); //            Shift(Base, -3)
        if (BackgroundLight == default)
            BackgroundLight = Shift(Background, 1); //  Shift(Base, -2)
        if (BackgroundDark == default)
            BackgroundDark = Shift(Background, -1); //  Shift(Base, -4)

        if (Text == default)
            Text = Base; //                             Shift(Base,  0)
        if (TextDark == default)
            TextDark = Shift(Text, -1); //              Shift(Base, -1)

        return this;
    }

    private Color Shift(Color from, float factor)
    {
        return from.NudgeLightness(LightnessShift * factor).NudgeChroma(ChromaShift * factor);
    }

    public Color Element;
    public Color HoveredElement;
    public Color PressedElement;
    public Color DisabledElement;

    public Color Background;
    public Color BackgroundLight;
    public Color BackgroundDark;

    public Color Text;
    public Color TextDark;
}
