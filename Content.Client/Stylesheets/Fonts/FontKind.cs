namespace Content.Client.Stylesheets.Fonts;

/// <summary>
///     The available kinds of font.
/// </summary>
public enum FontKind
{
    Regular,
    Bold,
    Italic,
    BoldItalic,
}

public static class FontKindExtensions
{
    internal static string AsFileName(this FontKind kind)
    {
        return kind switch
        {
            FontKind.Regular => "Regular",
            FontKind.Bold => "Bold",
            FontKind.Italic => "Italic",
            FontKind.BoldItalic => "BoldItalic",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    internal static bool IsBold(this FontKind kind)
    {
        return kind is FontKind.Bold or FontKind.BoldItalic;
    }

    internal static bool IsItalic(this FontKind kind)
    {
        return kind is FontKind.Italic or FontKind.BoldItalic;
    }

    internal static FontKind SimplifyCompound(this FontKind kind)
    {
        return kind switch
        {
            FontKind.BoldItalic => FontKind.Bold,
            _ => kind,
        };
    }


    internal static FontKind RegularOr(this FontKind kind, FontKind other)
    {
        return kind switch
        {
            FontKind.Regular => FontKind.Regular,
            _ => other,
        };
    }
}

