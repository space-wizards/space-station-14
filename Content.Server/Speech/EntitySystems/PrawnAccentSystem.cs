using System.Globalization;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class PrawnAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "simple", "shrimple" },
        { "space", "the sea" },
        { "me", "prawn" },
        { "kid", "squid" },
        { "confusing", "conchfusing" },
        { "welcome", "whalecome" },
        { "died", "drowned" },
        { "agency", "agensea"},
        { "latency", "latensea" },
        { "specific", "pacific"},
        { "send", "sand"},
        { "crazy", "craysea" },
        { "lazy", "laysea" },
        { "clown", "clownfish" },
        { "killed", "krilled" },
        { "sell", "sail" },
        { "easy", "shrimple"},
        { "combine", "combrine" },
        { "mine", "brine" },
        { "whiskey", "whisksea" },
        { "lobster", "shrobster" },
        { "what do", "water" },
        { "sure", "shore" },
        { "salt", "sea salt" },
        { "dog", "dogfish" },
        { "see", "sea" },
        { "finger", "fish finger" },
        { "confiscate", "confishcake" },
        { "enemy", "anemone" },
        { "help", "kelp" },
        { "kill", "krill" },
        { "what is", "waters" },
        { "imp", "shrimp" },
	    { "i", "prawn" },
	    { "you see", "sushi" },
	    { "cheese", "seas" },
	    { "purpose", "porpoise" },
	    { "dead", "drowned" },
	    { "dying", "drowning" },
	    { "selfish", "shelfish" },
	    { "hi", "Ahoy" },
	    { "he", "fish" },
	    { "her", "crab" },
	    { "lung", "gill" },
	    { "lungs", "gills" },
	    { "important", "shrimportant" },
	    { "impossible", "shrimpossible" },
	    { "improbable", "shrimprobable" },
	    { "security", "sharkurity" },
	    { "helping", "kelping" },
	    { "missed", "fished" },
	    { "im", "prawn" },
	    { "hello", "Ahoy" },
	    { "me?", "prawn" },
	    { "shuttle", "shrimp boat" },
	    { "i'm", "prawn" },
	    { "float", "swim" },
	    { "floating", "swimming" },
	    { "water", "salt water" },
	    { "cat", "catfish" },
	    { "mime", "mimefish" },
	    { "shrimp", "delicious shrimp" },
	    { "mouse", "food" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrawnAccentComponent, AccentGetEvent>(OnAccentGet);
    }
    private void OnAccentGet(EntityUid uid, PrawnAccentComponent component, AccentGetEvent args)
    {
        foreach (var (first, replace) in DirectReplacements)
        {
            args.Message = Regex.Replace(args.Message, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }
    }
}