using System.Collections.Generic;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    // TODO: Code in-game languages and make this a language
    public class CatAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Meow = new List<string>{ "Meow!", "Mow.", "Mrrrow!", "Hhsss!", "Brrow?" };

        public override void Initialize()
        {
            SubscribeLocalEvent<CatAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // TODO: Maybe add more than one squeek when there are more words?
            return _random.Pick(Meow);
        }

        private void OnAccent(EntityUid uid, CatAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
