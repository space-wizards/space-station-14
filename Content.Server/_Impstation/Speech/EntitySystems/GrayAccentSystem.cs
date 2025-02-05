using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class GrayAccentComponentAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexAmContraction = new(@"([a-z])'[Mm]\b");
    private static readonly Regex RegexAmContractionUpper = new(@"([A-Z])'[Mm]\b");
    private static readonly Regex RegexAreContraction = new(@"([a-z])'[Rr][Ee]\b");
    private static readonly Regex RegexAreContractionUpper = new(@"([A-Z])'[Rr][Ee]\b");
    private static readonly Regex RegexThuiLower = new(@"(.)\b[Tt]hui\b");
    private static readonly Regex RegexThuiUpperLeft = new(@"(\b[A-Z]+.)\b[Tt]hui\b");
    private static readonly Regex RegexThuiUpperRight = new(@"\b[Tt]hui\b(.[A-Z]+\b)");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrayAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, GrayAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "gray_accent");

        message = RegexAmContraction.Replace(message, "$1-wa");
        message = RegexAmContractionUpper.Replace(message, "$1-WA");
        message = RegexAreContraction.Replace(message, "$1zz");
        message = RegexAreContractionUpper.Replace(message, "$1ZZ");
        message = RegexThuiLower.Replace(message, "$1thui");
        message = RegexThuiUpperLeft.Replace(message, "$1THUI");
        message = RegexThuiUpperRight.Replace(message, "THUI$1");

        args.Message = message;
    }
}
