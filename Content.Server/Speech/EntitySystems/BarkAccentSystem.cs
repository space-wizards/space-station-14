using Content.Shared.StatusEffectNew;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class BarkAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Barks = new List<string>{
            " Woof!", " WOOF", " wof-wof"
        }.AsReadOnly();

        private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
        {
            { "ah", "arf" },
            { "Ah", "Arf" },
            { "oh", "oof" },
            { "Oh", "Oof" },
        };

        public override void Initialize()
        {
            SubscribeLocalEvent<BarkAccentComponent, AccentGetEvent>(OnAccent);
            SubscribeLocalEvent<BarkAccentComponent, StatusEffectRelayedEvent<AccentGetEvent>>(OnAccentRelayed);
        }

        public string Accentuate(string message)
        {
            foreach (var (word, repl) in SpecialWords)
            {
                message = message.Replace(word, repl);
            }

            return message.Replace("!", _random.Pick(Barks))
                .Replace("l", "r").Replace("L", "R");
        }

        private void OnAccent(Entity<BarkAccentComponent> entity, ref AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }

        private void OnAccentRelayed(Entity<BarkAccentComponent> entity, ref StatusEffectRelayedEvent<AccentGetEvent> args)
        {
            args.Args.Message = Accentuate(args.Args.Message);
        }
    }
}
