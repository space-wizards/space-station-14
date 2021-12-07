using System.Collections.Generic;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    // TODO: Code in-game languages and make this a language
    public class MonkeyAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Bark = new List<string>{ "Bark!", "Bork!", "Woof!", "Arf.", "Grrr." };

        public override void Initialize()
        {
            SubscribeLocalEvent<MonkeyAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // TODO: Maybe add more than one squeek when there are more words?
            return _random.Pick(Bark);
        }

        private void OnAccent(EntityUid uid, MonkeyAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
