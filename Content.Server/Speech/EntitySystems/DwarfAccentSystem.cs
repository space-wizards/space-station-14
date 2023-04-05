using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class DwarfAccentSystem : EntitySystem
{
    // TODO:
    // these are pretty bad to have as static dicts in systems, ideally these all get moved to prototypes
    // these can honestly stay unlocalized in prototypes? -- most of these word-replacers make zero sense to localize into other languages
    // since they're so english-specific
    // all of the 'word-replacers' should also probably respect capitalization when transformed, so all caps -> all caps
    // and first letter capitalized -> first letter capitalized, at the very least

    // these specifically mostly come from examples of specific scottish-english (not necessarily scots) verbiage
    // https://en.wikipedia.org/wiki/Scotticism
    // https://en.wikipedia.org/wiki/Scottish_English
    // https://www.cs.stir.ac.uk/~kjt/general/scots.html
    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "girl", "lassie" },
        { "boy", "laddie" },
        { "man", "lad" },
        { "woman", "lass" },
        { "do", "dae" },
        { "don't", "dinnae" },
        { "dont", "dinnae" },
        { "i'm", "A'm" },
        { "im", "am"},
        { "going", "gaun" },
        { "know", "ken"},
        { "i", "Ah" },
        { "you're", "ye're"},
        { "youre", "yere"},
        { "you", "ye" },
        { "i'll", "A'll" },
        { "ill", "all"},
        { "of", "ae" },
        { "was", "wis" },
        { "can't", "cannae" },
        { "cant", "cannae" },
        { "yourself", "yersel" },
        { "where", "whaur" },
        { "oh", "ach" },
        { "little", "wee" },
        { "small", "wee" },
        { "shit", "shite" },
        { "yeah", "aye" },
        { "yea", "aye"},
        { "yes", "aye" },
        { "too", "tae" },
        { "my", "ma" },
        { "not", "nae" },
        { "dad", "da" },
        { "mom", "maw" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DwarfAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message)
    {
        // this is just word replacements right now,
        // but leaving it open to more intelligent phonotactic manipulations at some point which are probably possible
        var msg = message;

        foreach (var (first, replace) in DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, DwarfAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
