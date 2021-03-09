using System;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Random;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class MouseAccentComponent : Component, IAccentComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override string Name => "MouseAccent";

        private static readonly IReadOnlyList<string> Squeek = new List<string>{
            "Squeak!", "Piep!", "Chuu!", "Chilla!"
        }.AsReadOnly();
        private string RandomSqueek => _random.Pick(Squeek);

        public string Accentuate(string message)
        {
            var squeak = message.Replace(message, RandomSqueek);
            return new string(squeak);
        }
    }
}
