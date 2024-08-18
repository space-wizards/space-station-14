using System.Text;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class GlitchAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Formats = new List<string>{
            "::", "-", "_", ".", "/", ""
        }.AsReadOnly();

        public override void Initialize()
        {
            SubscribeLocalEvent<GlitchAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            if (_random.NextDouble() >= 0.7)
                return message;
            else if (_random.NextDouble() >= 0.3)
                return message.Replace(" ", _random.Pick(Formats));
            else
            {
                var words = message.Split();
                var accentedMessage = new StringBuilder(message.Length + 2);

                for (var i = 0; i < words.Length; i++)
                {
                    var word = words[i];

                    if (_random.NextDouble() >= 0.75)
                    {
                        if (_random.NextDouble() >= 0.3)
                        {
                            foreach (var letter in word)
                            {
                                if (_random.NextDouble() >= 0.8)
                                    accentedMessage.Append('*');
                                else
                                    accentedMessage.Append(letter);
                            }
                        }
                        else if (_random.NextDouble() >= 0.5)
                        {
                            accentedMessage.Append("ERROR");
                        }
                        else
                        {
                            foreach (var _ in word)
                            {
                                if (_random.NextDouble() >= 0.5)
                                    accentedMessage.Append('0');
                                else
                                    accentedMessage.Append('1');
                            }
                        }
                    }
                    else
                    {
                        accentedMessage.Append(word);
                    }

                    if (i < words.Length - 1)
                        accentedMessage.Append(' ');
                }
                return accentedMessage.ToString();
            }
        }

        private void OnAccent(EntityUid uid, GlitchAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
