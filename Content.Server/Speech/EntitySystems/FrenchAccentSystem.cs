using Content.Server.Speech.Components;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class FrenchAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrenchAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, FrenchAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "french");

        // replaces th with dz 
        msg = Regex.Replace(msg, @"th", "'z", RegexOptions.IgnoreCase);

        // removes the letter h from the start of words.
        msg = Regex.Replace(msg, @"(?<!\w)[h]", "'", RegexOptions.IgnoreCase);

        // spaces out ! ? and ,.
        msg = Regex.Replace(msg, @"(?<=\w\w)!(?!\w)", " !", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<=\w\w)[?](?!\w)", " ?", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<=\w\w)[,](?!\w)", " ,", RegexOptions.IgnoreCase);

        return msg;
    }

    private void OnAccentGet(EntityUid uid, FrenchAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
