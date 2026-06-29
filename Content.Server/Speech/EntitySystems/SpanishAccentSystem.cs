using System.Text.RegularExpressions;
using System.Text;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        private static readonly Regex RegexLower = new(@"(?<!\w)(s+h*[bcdfgjklmnpqrtvwxz])"); // for words in all lowercase (multiple "s" are allowed)
        private static readonly Regex RegexCaps = new(@"(?<!\w)S(s*h*[bcdfgjklmnpqrtvwxz])"); // For Capitalized Words (Station -> Estation; Capital "S" Is Replaced Directly, Multiple "S" Are Allowed)
        private static readonly Regex RegexUpper = new(@"(?<!\w)(SH*[BCDFGJKLMNPQRTVWXZ])"); // FOR WORDS IN ALL UPPERCASE (ONLY ONE "S" IS ALLOWED, ASSUMING IT'S NOT AN ACRONYM)

        public override void Initialize()
        {
            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // Insert E before every S that is followed by a consonant that isn't H ([sh] makes a single sound)
            message = InsertS(message);
            // If a sentence ends with ?, insert a reverse ? at the beginning of the sentence
            message = ReplacePunctuation(message);
            return message;
        }

        private string InsertS(string message)
        {
            // Replace every new Word that starts with s/S and a consonant
            message = RegexLower.Replace(message, "e$1");
            message = RegexCaps.Replace(message, "Es$1");
            message = RegexUpper.Replace(message, "E$1");

            return message;
        }

        private string ReplacePunctuation(string message)
        {
            var sentences = AccentSystem.SentenceRegex.Split(message);
            var msg = new StringBuilder();
            foreach (var s in sentences)
            {
                var toInsert = new StringBuilder();
                for (var i = s.Length - 1; i >= 0 && "?!‽".Contains(s[i]); i--)
                {
                    toInsert.Append(s[i] switch
                    {
                        '?' => '¿',
                        '!' => '¡',
                        '‽' => '⸘',
                        _ => ' '
                    });
                }
                if (toInsert.Length == 0)
                {
                    msg.Append(s);
                } else
                {
                    msg.Append(s.Insert(s.Length - s.TrimStart().Length, toInsert.ToString()));
                }
            }
            return msg.ToString();
        }

        private void OnAccent(EntityUid uid, SpanishAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
