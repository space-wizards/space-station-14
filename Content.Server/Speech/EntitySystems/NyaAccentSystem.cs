using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class NyaAccentSystem : EntitySystem
{   
    [Dependency] private readonly IRobustRandom _random = default!;
    
    private static readonly Dictionary<string, string> DirectReplacements = new() {
        {"fuck you", "hiiiss" },
        {"fuck", "fish" },
        {"fck", "fsh" },
        {"shit", "damn" },
        {"stupid", "baka" },

        {"now", "nyow"},
        {"many", "meuny"},
        {"machine", "meochine"},
        {"magic", "mewgic"},

        {"maint", "meoint"},
        {"maintenance", "meowintenance"},
        {"manifest", "meownifest"},

        {"syndicate", "syndiCat"},
        {"nanotrasen", "nyanotrasen"},
        {"drugs", "cat mint"}
        
    };

    private static readonly IReadOnlyList<string> Ending = new List<string> {
        "nya",
        "meow",
        "mew",
        "mrrr"
    }.AsReadOnly();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NyaAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message) {
        var final_msg = "";

        // Sentence ending
        var sentences = AccentSystem.SentenceRegex.Split(message);
        foreach (var s in sentences)
        {   
            var new_s = s;

            if (!string.IsNullOrWhiteSpace(new_s) && _random.Prob(0.5f))
            {   
                // Logger.DebugS("nya", $"SENTENCE: {new_s}");

                string last_sym = new_s.Substring(new_s.Length-1);
                string punct_mark = "";
                string insert = _random.Pick(Ending);

                // Checking punctuation at end of the sentence
                if(Regex.Matches(last_sym, "[!?.]").Count > 0)
                {
                    punct_mark = last_sym;
                    new_s = new_s.Remove(new_s.Length-1);
                }

                // Add comma if "s" is real sentence
                if (!new_s.EndsWith(' ')) {
                    insert = " " + insert;
                    if (new_s.Length > 0 && char.IsLetterOrDigit(new_s, new_s.Length-1))
                    {
                        insert = "," + insert;
                    }
                }

                // Insert ending word
                new_s += insert + punct_mark;
            }
            final_msg += new_s;
        }

        // Direct replacements
        foreach (var (first, replace) in DirectReplacements)
        {
            //final_msg = Regex.Replace(final_msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
            final_msg = final_msg.Replace(first, replace, true, null);
        }

        //Some word parts replacements
        final_msg = final_msg.Replace("per", "pro");
        final_msg = final_msg.Replace("Per", "Pro");

        final_msg = final_msg.Replace("pro", "pro");
        final_msg = final_msg.Replace("Pro", "Pro");
        
        final_msg = final_msg.Replace("mor", "murr");
        final_msg = final_msg.Replace("Mur", "Murr");

        final_msg = final_msg.Replace("mo", "meo");
        final_msg = final_msg.Replace("Mo", "Meo");

        // Some things
        final_msg = final_msg.Trim();
        final_msg = char.ToUpper(final_msg[0]) + final_msg.Substring(1);

        return final_msg;
    }

    private void OnAccent(EntityUid uid, NyaAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}

//  |\__/,|   (`\
//  |_ _  |.--.) )
//  ( T   )     /
// (((^_(((/(((_>
