using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;

namespace Content.Shared.Speech.Accents;

public sealed class FrenchAccent : IAccent
{
    public string Name { get; } = "French";

    [Dependency] private readonly SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexTh = new(@"th", RegexOptions.IgnoreCase);
    private static readonly Regex RegexStartH = new(@"(?<!\w)h", RegexOptions.IgnoreCase);
    private static readonly Regex RegexSpacePunctuation = new(@"(?<=\w\w)[!?;:](?!\w)", RegexOptions.IgnoreCase);

    public string Accentuate(string message, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
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
}
