using System.Text.RegularExpressions;
using System.Text;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        private static readonly Regex RegexLowersC = new(@"(?<!\w)(s+h*[bcdfgjklmnpqrtvwxz])");
        private static readonly Regex RegexUpperSC = new(@"(?<!\w)S(s*h*[bcdfgjklmnpqrtvwxz])");
        private static readonly Regex RegexCapsSC = new(@"(?<!\w)(SH*[BCDFGJKLMNPQRTVWXZ])");

        public override void Initialize()
        {
            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // Insert E before every S that is followed by a consonant
            message = InsertS(message);
            // If a sentence ends with ?, insert a reverse ? at the beginning of the sentence
            message = ReplacePunctuation(message);
            return message;
        }

        private string InsertS(string message)
        {
            // Replace every new Word that starts with s/S and a consonant
            var msg = message;

            msg = RegexLowersC.Replace(msg, "e$1");
            msg = RegexUpperSC.Replace(msg, "Es$1");
            msg = RegexCapsSC.Replace(msg, "E$1");

            return msg;
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
