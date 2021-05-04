using System;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Random;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class CowAccentComponent : Component, IAccentComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override string Name => "CowAccent";

        private static readonly IReadOnlyList<string> Moo = new List<string>{
            "Moo!", "Mooo!", "Mooooo!", "Moo-mu!", "Mu-moo!" 
        }.AsReadOnly();

        public string Accentuate(string message)
        {
            return _random.Pick(Moo);
        }
    }
}
