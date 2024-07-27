using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SouthernAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerIng = new(@"ing\b");
    private static readonly Regex RegexUpperIng = new(@"ING\b");
    private static readonly Regex RegexLowerAnd = new(@"\band\b");
    private static readonly Regex RegexUpperAnd = new(@"\bAND\b");
    private static readonly Regex RegexLowerDve = new(@"d've\b");
    private static readonly Regex RegexUpperDve = new(@"D'VE\b");

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SouthernAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SouthernAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "southern");

        //They shoulda started runnin' an' hidin' from me!
        message = RegexLowerIng.Replace(message, "in'");
        message = RegexUpperIng.Replace(message, "IN'");
        message = RegexLowerAnd.Replace(message, "an'");
        message = RegexUpperAnd.Replace(message, "AN'");
        message = RegexLowerDve.Replace(message, "da");
        message = RegexUpperDve.Replace(message, "DA");
        args.Message = message;
    }
};
