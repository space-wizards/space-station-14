using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class SouthernAccentSystem : RelayAccentSystem<SouthernAccentComponent>
{
    private static readonly Regex RegexLowerIng = new(@"ing\b");
    private static readonly Regex RegexUpperIng = new(@"ING\b");
    private static readonly Regex RegexLowerAnd = new(@"\band\b");
    private static readonly Regex RegexUpperAnd = new(@"\bAND\b");
    private static readonly Regex RegexLowerDve = new(@"d've\b");
    private static readonly Regex RegexUpperDve = new(@"D'VE\b");

    [Dependency] private ReplacementAccentSystem _replacement = default!;

    public override string Accentuate(string message, Entity<SouthernAccentComponent>? _)
    {
        message = _replacement.ApplyReplacements(message, "southern");

        //They shoulda started runnin' an' hidin' from me!
        message = RegexLowerIng.Replace(message, "in'");
        message = RegexUpperIng.Replace(message, "IN'");
        message = RegexLowerAnd.Replace(message, "an'");
        message = RegexUpperAnd.Replace(message, "AN'");
        message = RegexLowerDve.Replace(message, "da");
        message = RegexUpperDve.Replace(message, "DA");
        return message;
    }
};
