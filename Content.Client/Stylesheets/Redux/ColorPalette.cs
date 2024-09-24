using Content.Client.Stylesheets.Redux.Colorspace;

// ReSharper disable MemberCanBePrivate.Global

namespace Content.Client.Stylesheets.Redux;

/// <remarks>
///     Don't be afraid to add a lot of fields here! This class is made for readability.
/// </remarks>
public sealed class ColorPalette
{
    /// <summary>
    ///     The root color all others are derived from
    /// </summary>
    public Color Base;

    public float LightnessPositiveShift = 0.06f;
    public float LightnessNegativeShift = -0.06f;
    public float ChromaPositiveShift = 0.00f;
    public float ChromaNegativeShift = 0.00f;

    public ColorPalette(Color baseColor)
    {
        Base = baseColor;

        Element = Shift(Base, -1); //               Shift(Base, -1)
        HoveredElement = Shift(Element, 1); //      Shift(Base,  0)
        PressedElement = Shift(Element, -1); //     Shift(Base, -2)
        DisabledElement = Shift(Element, -3); //    Shift(Base, -4)

        Background = Shift(Base, -3); //            Shift(Base, -3)
        BackgroundLight = Shift(Background, 1); //  Shift(Base, -2)
        BackgroundDark = Shift(Background, -1); //  Shift(Base, -4)

        Text = Base; //                             Shift(Base,  0)
        TextDark = Shift(Text, -1); //              Shift(Base, -1)
    }

    private Color Shift(Color from, float factor)
    {
        return factor > 0
            ? from.NudgeLightness(LightnessPositiveShift * factor).NudgeChroma(ChromaPositiveShift * factor)
            : from.NudgeLightness(LightnessNegativeShift * factor).NudgeChroma(ChromaNegativeShift * factor);
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
