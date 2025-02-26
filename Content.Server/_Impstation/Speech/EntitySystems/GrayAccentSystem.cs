using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class GrayAccentComponentAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexPuUpperLeft = new(@"(?<=\b[A-Z]+.)\b[Pp]u\b");
    private static readonly Regex RegexPuUpperRight = new(@"\b[Pp]u\b(?=.[A-Z]+\b)");
    private static readonly Regex RegexThuiLower = new(@"(?<!^)(?<!\.\s+)\b[Tt]hui\b");
    private static readonly Regex RegexThuiUpperLeft = new(@"(?<=\b[A-Z]+.)\b[Tt]hui\b");
    private static readonly Regex RegexThuiUpperRight = new(@"\b[Tt]hui\b(?=.[A-Z]+\b)");
    private static readonly Regex RegexAmContraction = new(@"(?<=[a-z])'[Mm]\b");
    private static readonly Regex RegexAmContractionUpper = new(@"(?<=[A-Z])'[Mm]\b");
    private static readonly Regex RegexAreContraction = new(@"(?<=[a-z])'[Rr][Ee]\b");
    private static readonly Regex RegexAreContractionUpper = new(@"(?<=[A-Z])'[Rr][Ee]\b");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrayAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, GrayAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "gray_accent");

        message = RegexPuUpperLeft.Replace(message, "PU");
        message = RegexPuUpperRight.Replace(message, "PU");
        message = RegexThuiLower.Replace(message, "thui");
        message = RegexThuiUpperLeft.Replace(message, "THUI");
        message = RegexThuiUpperRight.Replace(message, "THUI");
        message = RegexAmContraction.Replace(message, "-wa");
        message = RegexAmContractionUpper.Replace(message, "-WA");
        message = RegexAreContraction.Replace(message, "zz");
        message = RegexAreContractionUpper.Replace(message, "ZZ");

        args.Message = message;
    }
}
