using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class OwOAccentSystem : EntitySystem
    {
        private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
        {
            { "you", "wu" },
            { "ты", "ти" }, // Corvax-Localization
            { "вы", "ви" },
        };

        public override void Initialize()
        {
            SubscribeLocalEvent<OwOAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            foreach (var (word, repl) in SpecialWords)
            {
                message = message.Replace(word, repl);
            }

            return message
                // Corvax-Localization-Start
                .Replace("р", "в").Replace("Р", "В")
                .Replace("л", "в").Replace("Л", "В")
                // Corvax-Localization-End
                .Replace("r", "w").Replace("R", "W")
                .Replace("l", "w").Replace("L", "W");
        }

        private void OnAccent(EntityUid uid, OwOAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
