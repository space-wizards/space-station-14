using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.Components
{
    [RegisterComponent]
    public class MouseAccentComponent : Component, IAccentComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override string Name => "MouseAccent";

        private static readonly IReadOnlyList<string> Squeek = new List<string>{
            "Squeak!", "Piep!", "Chuu!"
        }.AsReadOnly();

        public string Accentuate(string message)
        {
            return _random.Pick(Squeek);
        }
    }
}
