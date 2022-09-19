using System.Globalization;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class PirateAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "hi", "Arghh" },
        { "hello", "Ahoy" },
        { "hey", "Gar" },
        { "yo", "Yarr" },
        { "friend", "matey" },
        { "crew", "me harties" },
        { "yes", "aye" },
        { "yes captain", "aye aye captain"},
        { "hand hoy", "emergency" },
        { "get ready", "batten down the hatches"},
        { "wow", "blimey"},
        { "pirate", "buccaneer" },
        { "song", "sea shanty" },
        { "beat", "flogged" },
        { "beaten", "flogged" },
        { "beating", "flogging" },
        { "drink", "grog"},
        { "stop", "heave to" },
        { "go", "heave ho" },
        { "kid", "lad" },
        { "she", "wench" },
        { "shes", "wench is'" },
        { "left behind", "marooned" },
        { "do you get it?", "savvy?" },
        { "you get it?", "savvy?" },
        { "get it?", "savvy?" },
        { "understood?", "savvy?" },
        { "clown", "jolly roger" },
        { "mime", "scallywag" },
        { "i slipped", "lost me sea legs" },
        { "he slipped", "lost his sea legs" },
        { "drunk", "squiffy" },
        { "mop", "swab" },
	    { "clean", "swab" },
	    { "woman", "wench" },
	    { "lol", "yo ho ho" },
	    { "syndie", "sea dog" },
	    { "syndies", "sea dogs" },
	    { "hes dead", "hes in Davy Jones' locker" },
	    { "rat king", "big bilge rat" },
	    { "cheat", "hornswaggle" },
	    { "mate", "bucko" },
	    { "nonesense", "gibberish" },
	    { "ghost", "dredgie" },
	    { "discuss", "parley" },
	    { "lucky", "pistol proof" },
	    { "thief", "rapscallion" },
	    { "trick", "run a rig" },
	    { "security", "jack tar" },
	    { "sec", "jack tar" },
	    { "broken", "ramshackle" },
	    { "nukie", "scourge" },
	    { "wake up", "show a leg" },
	    { "joking me?", "sinking me?" },
	    { "are you joking?", "are you sinking me?" },
	    { "clothes", "togs" },
	    { "you", "ya" },
	    { "shuttle", "ship" },
	    { "space", "the sea" },
	    { "quickly", "step to" },
	    { "hurry", "smartly" },
	    { "spaced", "shark baited" },
	    { "mouse", "bilge rat" },
	    { "crewmembers", "hands" },
	    { "cry", "hang the jib" },
	    { "crying", "hanging the jib" },
	    { "bar", "SCUMM bar" },
	    { "fuck you", "ya fight like a dairy farmer" },
	    { "maintenance", "bilge" },
	    { "kill him", "blow the man down" },
	    { "egg", "cackle fruit" },
	    { "sword", "cutlass" },
	    { "food", "gruel" },
	    { "objective", "rutter" },
	    { "objectives", "rutters" },
	    { "feeling sick", "got scurvy" },
	    { "im sick", "got scurvy" },
	    { "omg", "shiver me timbers" },
	    { "stop the ship", "drop anchor" },
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
