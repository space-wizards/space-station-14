using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class ScottishAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerIng = new(@"ing\b");
    private static readonly Regex RegexUpperIng = new(@"ING\b");
    private static readonly Regex RegexLowerAnd = new(@"\band\b");
    private static readonly Regex RegexUpperAnd = new(@"\bAND\b");
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScottishAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, ScottishAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "scottish");

        message = RegexLowerIng.Replace(message, "in'");
        message = RegexUpperIng.Replace(message, "IN'");
        message = RegexLowerAnd.Replace(message, "an'");
        message = RegexUpperAnd.Replace(message, "AN'");
        args.Message = message;
    }
};
