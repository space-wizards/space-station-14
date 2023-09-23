using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class OwOAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Faces = new List<string>{
            " (@`ω´@)", " ;;w;;", " owo", " UwU", " >w<", " ^w^", ":3"
        }.AsReadOnly();

        private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
        {
            { "you", "wu" },
			{ "ты", "ти" },
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

            return message.Replace("!", _random.Pick(Faces))
                .Replace("r", "w").Replace("R", "W")
                .Replace("l", "w").Replace("L", "W")
				
                .Replace("р", "в").Replace("Р", "В")
                .Replace("л", "в").Replace("Л", "В")
                .Replace("на", "ня").Replace("На", "Ня").Replace("нА", "нЯ").Replace("НА", "НЯ")
                .Replace("ма", "мя").Replace("Ма", "Мя").Replace("мА", "мЯ").Replace("МА", "МЯ")
                .Replace("!", "~!").Replace("?", "~?")
                .Replace("-!", "~!").Replace("-?", "~?")
                .Replace("с", "ф").Replace("С", "Ф");
        }

        private void OnAccent(EntityUid uid, OwOAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
