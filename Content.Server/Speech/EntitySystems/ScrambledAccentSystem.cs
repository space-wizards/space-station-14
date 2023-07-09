using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class ScrambledAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ScrambledAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            var words = message.ToLower().Split();

            if (words.Length < 2)
            {
                var pick = _random.Next(1, 8);
                // If they try to weasel out of it by saying one word at a time we give them this.
                return Loc.GetString($"accent-scrambled-words-{pick}");
            }

            // Scramble the words
            var scrambled = words.OrderBy(x => _random.Next()).ToArray();

            var msg = string.Join(" ", scrambled);

            // First letter should be capital
            msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);

            // Capitalize lone i's
            msg = Regex.Replace(msg, @"(?<=\ )i(?=[\ \.\?]|$)", "I");
            return msg;
        }

        private void OnAccent(EntityUid uid, ScrambledAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
