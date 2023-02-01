using System.Globalization;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class PirateAccentSystem : EntitySystem
{
    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "hi", "Ahoy" },
        { "hello", "Ahoy" },
        { "hey", "Gar" },
        { "yo", "Yarr" },
        { "friend", "matey" },
        { "crew", "me hearties" },
        { "yes", "aye" },
        { "yes captain", "aye aye captain"},
        { "wow", "blimey"},
        { "pirate", "buccaneer" },
        { "song", "sea shanty" },
        { "stop", "heave to" },
        { "go", "heave ho" },
        { "understood", "savvy" },
        { "understand", "savvy" },
        { "drunk", "squiffy" },
	    { "clean", "swab" },
	    { "broken", "ramshackle" },
	    { "you", "ya" },
	    { "shuttle", "ship" },
	    { "quickly", "step to" },
	    { "spaced", "shark baited" },
	    { "yea", "aye" },
	    { "wait", "hold" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateAccentComponent, AccentGetEvent>(OnAccentGet);
    }
    private void OnAccentGet(EntityUid uid, PirateAccentComponent component, AccentGetEvent args)
    {
        foreach (var (first, replace) in DirectReplacements)
        {
            args.Message = Regex.Replace(args.Message, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }
    }
}
