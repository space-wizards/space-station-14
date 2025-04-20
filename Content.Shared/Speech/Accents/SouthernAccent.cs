using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class SouthernAccent : IAccent
{
    public string Name { get; } = "Southern";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexLowerIng = new(@"ing\b");
    private static readonly Regex RegexUpperIng = new(@"ING\b");
    private static readonly Regex RegexLowerAnd = new(@"\band\b");
    private static readonly Regex RegexUpperAnd = new(@"\bAND\b");
    private static readonly Regex RegexLowerDve = new(@"d've\b");
    private static readonly Regex RegexUpperDve = new(@"D'VE\b");

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();

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

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}
