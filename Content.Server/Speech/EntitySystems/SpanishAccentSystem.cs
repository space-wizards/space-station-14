using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        private static readonly Regex RegexWordInitialLowerCaseS = new("(^| )s");
        private static readonly Regex RegexWordInitialUpperCaseSNotFollowedByCapital = new("(^| )S([^A-Z])");
        private static readonly Regex RegexWordInitialUpperCaseSFollowedByCapital = new("(^| )S([A-Z])");

        private static readonly Regex RegexHaha = new(@"\b(ha)+\b");

        public override void Initialize()
        {
            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
        }

        public static string Accentuate(string message)
        {
            // Insert E before every S
            message = ReplaceWordInitialSWithEs(message);

            // Replace things like "haha"s with "jaja"s.
            message = ReplaceHaWithJa(message);

            // If a sentence ends with ?, insert a reverse ? at the beginning of the sentence
            message = ReplacePunctuation(message);
            return message;
        }

        private static string ReplaceWordInitialSWithEs(string message)
        {
            // stun -> estun
            message = RegexWordInitialLowerCaseS.Replace(message, "$1es");
            // Stun -> Estun
            message = RegexWordInitialUpperCaseSNotFollowedByCapital.Replace(message, "$1Es$2");
            // STUN -> ESTUN
            message = RegexWordInitialUpperCaseSFollowedByCapital.Replace(message, "$1ES$2");

            return message;
        }

        private static string ReplaceHaWithJa(string message)
        {
            if (RegexHaha.IsMatch(message))
            {
                // Preserve case and number of "ha"s by replacing individual letters.
                message = message.Replace('h', 'j').Replace('H', 'j');
            }

            return message;
        }

        private static string ReplacePunctuation(string message)
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
                        _ => ' ',
                    });
                }

                msg.Append(toInsert.Length == 0 ? s : s.Insert(s.Length - s.TrimStart().Length, toInsert.ToString()));
            }

            return msg.ToString();
        }

        private static void OnAccent(Entity<SpanishAccentComponent> entity, ref AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
