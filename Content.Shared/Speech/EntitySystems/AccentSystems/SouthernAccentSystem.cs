using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class SouthernAccentSystem : AccentSystem<SouthernAccentComponent>
{
    private static readonly Regex RegexLowerIng = new(@"ing\b");
    private static readonly Regex RegexUpperIng = new(@"ING\b");
    private static readonly Regex RegexLowerAnd = new(@"\band\b");
    private static readonly Regex RegexUpperAnd = new(@"\bAND\b");
    private static readonly Regex RegexLowerDve = new(@"d've\b");
    private static readonly Regex RegexUpperDve = new(@"D'VE\b");

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override string Accentuate(Entity<SouthernAccentComponent>? entity, string message)
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
