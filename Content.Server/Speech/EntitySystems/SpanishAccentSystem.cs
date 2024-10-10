using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

        private static readonly Regex RegexWordInitialLowerCaseS = new(@"(^| )s");
        private static readonly Regex RegexWordInitialUpperCaseSNotFollowedByCapital = new(@"(^| )S([^A-Z])");
        private static readonly Regex RegexWordInitialUpperCaseSFollowedByCapital = new(@" (^| )([A-Z])");

        private static readonly Regex RegexHaha = new(@"\b(ha)+\b");

        public override void Initialize()
        {
            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // Insert E before every S
            message = ReplaceWordInitialSWithEs(message);
            // Replace things like "haha"s with "jaja"s.
            message = ReplaceHaWithJa(message);

            // Replace words after the accent application so that the spnish words aren't "accented".
            // The downside of this is that the words-to-replace look for the "es-" modified words.
            message = _replacement.ApplyReplacements(message, "spanish");

            // If a sentence ends with ?, insert a reverse ? at the beginning of the sentence
            message = ReplacePunctuation(message);
            return message;
        }

        private string ReplaceWordInitialSWithEs(string message)
        {
            // stun -> estun
            message = RegexWordInitialLowerCaseS.Replace(message, "$1es");
            // Stun -> Estun
            message = RegexWordInitialUpperCaseSNotFollowedByCapital.Replace(message, "$1Es$2");
            // STUN -> ESTUN
            message = RegexWordInitialUpperCaseSFollowedByCapital.Replace(message, "$1ES$2");

            return message;
        }

        private string ReplaceHaWithJa(string message)
        {
            if (RegexHaha.IsMatch(message))
            {
                // Preserve case and number of "ha"s by replacing individual letters.
                message = message.Replace('h', 'j').Replace('H', 'j');
            }
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
                }
                else
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
