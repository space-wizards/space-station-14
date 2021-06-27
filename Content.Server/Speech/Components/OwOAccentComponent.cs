using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.Components
{
    [RegisterComponent]
    public class OwOAccentComponent : Component, IAccentComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "OwOAccent";

        private static readonly IReadOnlyList<string> Faces = new List<string>{
            " (・`ω´・)", " ;;w;;", " owo", " UwU", " >w<", " ^w^"
        }.AsReadOnly();
        private string RandomFace => _random.Pick(Faces);

        private static readonly Dictionary<string, string> SpecialWords = new()
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
