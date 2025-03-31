using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class FrenchAccent : IAccent
{
    public string Name { get; } = "French";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexTh = new(@"th", RegexOptions.IgnoreCase);
    private static readonly Regex RegexStartH = new(@"(?<!\w)h", RegexOptions.IgnoreCase);
    private static readonly Regex RegexSpacePunctuation = new(@"(?<=\w\w)[!?;:](?!\w)", RegexOptions.IgnoreCase);

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();

        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "french");

        // replaces th with z
        msg = RegexTh.Replace(msg, "'z");

        // replaces h with ' at the start of words.
        msg = RegexStartH.Replace(msg, "'");

        // spaces out ! ? : and ;.
        msg = RegexSpacePunctuation.Replace(msg, " $&");

        return msg;
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}
