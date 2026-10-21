using Content.Client.Stylesheets.Colorspace;

// ReSharper disable MemberCanBePrivate.Global

namespace Content.Client.Stylesheets.Palette;

/// <remarks>
///     Don't be afraid to add a lot of fields here! This class is made for readability.
/// </remarks>
public record ColorPalette(
    Color Base,

    float LightnessShift,
    float ChromaShift,

    Color Element,
    Color HoveredElement,
    Color PressedElement,
    Color DisabledElement,

    Color Background,
    Color BackgroundLight,
    Color BackgroundDark,

    Color Text,
    Color TextDark
)
{
    /// <summary>
    /// Helper method for generating a ColorPalette from a specified base hex string, with the
    /// option to override specific parts of the palette
    /// </summary>
    public static ColorPalette FromHexBase(
        string hex,
        float lightnessShift = 0.06f,
        float chromaShift = 0.00f,
        Color? element = null,
        Color? background = null,
        Color? text = null
    )
    {
        var @base = Color.FromHex(hex);

        element ??= Shift(@base, lightnessShift, chromaShift, -1); //                        Shift(@base, -1)
        var hoveredElement = Shift(element.Value, lightnessShift, chromaShift, 1); //        Shift(@base,  0)
        var pressedElement = Shift(element.Value, lightnessShift, chromaShift, -1); //       Shift(@base, -2)
        var disabledElement = Shift(element.Value, lightnessShift, chromaShift, -2) //       Shift(@base, -3)
            .NudgeChroma(-chromaShift * 2);

        background ??= Shift(@base, lightnessShift, chromaShift, -3); //                     Shift(@base, -3)
        var backgroundLight = Shift(background.Value, lightnessShift, chromaShift, 1); //    Shift(@base, -2)
        var backgroundDark = Shift(background.Value, lightnessShift, chromaShift, -1); //    Shift(@base, -4)

        text ??= @base; //                                                                   Shift(@base,  0)
        var textDark = Shift(text.Value, lightnessShift, chromaShift, -1); //                Shift(@base, -1)

        return new ColorPalette(
            Base: @base,

            LightnessShift: lightnessShift,
            ChromaShift: chromaShift,

            Element: element.Value,
            HoveredElement: hoveredElement,
            PressedElement: pressedElement,
            DisabledElement: disabledElement,

            Background: background.Value,
            BackgroundLight: backgroundLight,
            BackgroundDark: backgroundDark,

            Text: text.Value,
            TextDark: textDark
        );
    }

    private static Color Shift(Color from, float lightnessShift, float chromaShift, float factor)
    {
        return from.NudgeLightness(lightnessShift * factor).NudgeChroma(chromaShift * factor);
    }
}
