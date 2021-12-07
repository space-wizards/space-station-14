using System.Collections.Generic;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    // TODO: Code in-game languages and make this a language
    public class DogAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Bark = new List<string>{ "Bark!", "Bork!", "Woof!", "Arf.", "Grrr." };

        public override void Initialize()
        {
            SubscribeLocalEvent<DogAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // TODO: Maybe add more than one squeek when there are more words?
            return _random.Pick(Bark);
        }

        private void OnAccent(EntityUid uid, DogAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
