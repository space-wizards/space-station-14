using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class DwarfAccentComponentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexAhAmContractionLower = new(@"(?<!^)(?<!\.\s+)\b[Aa]'[Mm]\b");
    private static readonly Regex RegexAhAmContractionUpperLeft = new(@"(?<=\b[A-Z]+.)\b[Aa]'[Mm]\b");
    private static readonly Regex RegexAhAmContractionUpperRight = new(@"\b[Aa]'[Mm]\b(?=.[A-Z]+\b)");
    private static readonly Regex RegexAhLower = new(@"(?<!^)(?<!\.\s+)\b[Aa]h\b");
    private static readonly Regex RegexAhUpperLeft = new(@"(?<=\b[A-Z]+.)\b[Aa]h\b");
    private static readonly Regex RegexAhUpperRight = new(@"\b[Aa]h\b(?=.[A-Z]+\b)");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DwarfAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, DwarfAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "dwarf");

        message = RegexAhAmContractionLower.Replace(message, "ah be");
        message = RegexAhAmContractionUpperLeft.Replace(message, "AH BE");
        message = RegexAhAmContractionUpperRight.Replace(message, "AH BE");
        message = RegexAhLower.Replace(message, "ah");
        message = RegexAhUpperLeft.Replace(message, "AH");
        message = RegexAhUpperRight.Replace(message, "AH");

        args.Message = message;
    }
}
