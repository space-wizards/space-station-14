using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class OwOAccentComponent : Component, IAccentComponent
    {
        [Dependency] private readonly IRobustRandom _random;

        public override string Name => "OwOAccent";

        private static readonly IReadOnlyList<string> Faces = new List<string>{
            " (・`ω´・)", " ;;w;;", " owo", " UwU", " >w<", " ^w^"
        }.AsReadOnly();
        private string RandomFace => _random.Pick(Faces);

        private static readonly Dictionary<string, string> SpecialWords = new Dictionary<string, string>
        {
            { "you", "wu" },
        };

        public string Accentuate(string message)
        {
            foreach (var (word, repl) in SpecialWords)
            {
                message = message.Replace(word, repl);
            }

            return message.Replace("!", RandomFace)
                .Replace("r", "w").Replace("R", "W")
                .Replace("l", "w").Replace("L", "W");
        }
    }
}
