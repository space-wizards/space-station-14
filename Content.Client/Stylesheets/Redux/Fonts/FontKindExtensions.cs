using JetBrains.Annotations;

namespace Content.Client.Stylesheets.Redux.Fonts;

[PublicAPI]
public static class FontKindExtensions
{
    public static string AsFileName(this FontKind kind)
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

    public static bool IsBold(this FontKind kind)
    {
        return kind is FontKind.Bold or FontKind.BoldItalic;
    }

    public static bool IsItalic(this FontKind kind)
    {
        return kind is FontKind.Italic or FontKind.BoldItalic;
    }

    public static FontKind SimplifyCompound(this FontKind kind)
    {
        return kind switch
        {
            FontKind.BoldItalic => FontKind.Bold,
            _ => kind,
        };
    }


    public static FontKind RegularOr(this FontKind kind, FontKind other)
    {
        return kind switch
        {
            FontKind.Regular => FontKind.Regular,
            _ => other,
        };
    }
}
