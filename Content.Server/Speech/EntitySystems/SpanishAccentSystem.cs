using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // Insert E before every S
            message = InsertS(message);
            // If a sentence ends with ?, insert a reverse ? at the beginning of the sentence
            message = ReplaceQuestionMark(message);
            return message;
        }

        private string InsertS(string message)
        {
            // Replace every new Word that starts with s/S
            var msg = message.Replace(" s", " es").Replace(" S", " Es");

            // Still need to check if the beginning of the message starts
            if (msg.StartsWith("s", StringComparison.Ordinal))
            {
                return msg.Remove(0, 1).Insert(0, "es");
            }
            else if (msg.StartsWith("S", StringComparison.Ordinal))
            {
                return msg.Remove(0, 1).Insert(0, "Es");
            }

            return msg;
        }

        private string ReplaceQuestionMark(string message)
        {
            var sentences = AccentSystem.SentenceRegex.Split(message);
            var msg = "";
            foreach (var s in sentences)
            {
                if (s.EndsWith("?", StringComparison.Ordinal)) // We've got a question => add ¿ to the beginning
                {
                    // Because we don't split by whitespace, we may have some spaces in front of the sentence.
                    // So we add the symbol before the first non space char
                    msg += s.Insert(s.Length - s.TrimStart().Length, "¿");
                }
                else
                {
                    msg += s;
                }
            }
            return msg;
        }

        private void OnAccent(EntityUid uid, SpanishAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
