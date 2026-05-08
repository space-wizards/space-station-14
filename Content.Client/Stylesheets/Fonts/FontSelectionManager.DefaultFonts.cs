using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Fonts;

internal sealed partial class FontSelectionManager
{
    private static readonly FontFamilyStack DefaultFontMain = FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSans/NotoSans-Regular.ttf"))
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSans/NotoSans-Bold.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSans/NotoSans-Italic.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoEmoji.ttf"))
        .Build();

    private static readonly FontFamilyStack DefaultFontTitle = FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-Regular.ttf"))
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-Bold.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-Italic.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-BoldItalic.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoEmoji.ttf"))
        .Build();

    private static readonly FontFamilyStack DefaultFontMachineTitle = FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/Boxfont-round/Boxfont Round.ttf"))
        .Build();

    private static readonly FontFamilyStack DefaultFontMonospace = FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/RobotoMono/RobotoMono-Regular.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/RobotoMono/RobotoMono-Bold.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/RobotoMono/RobotoMono-Italic.ttf"))
        .Build();
}
