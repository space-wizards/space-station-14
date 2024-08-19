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
            var rand = _random.NextDouble();
            if (rand >= 0.7)
                return message;
            else if (rand >= 0.2)
                return message.Replace(" ", _random.Pick(Formats));
            return RebuildSentence(message);
        }

        private string RebuildSentence(string message)
        {
            var words = message.Split();
            var accentedMessage = new StringBuilder(message.Length + 2);

            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var rand = _random.NextDouble();

                if (rand >= 0.75)
                    accentedMessage.Append(AccentuateWord(word));
                else
                    accentedMessage.Append(word);

                if (i < words.Length - 1)
                    accentedMessage.Append(' ');
            }
            return accentedMessage.ToString();
        }

        private string AccentuateWord(string word)
        {
            var rand = _random.NextDouble();
            if (rand >= 0.3)
                return AddStarsToWord(word);
            else if (rand >= 0.15)
                return ChangeWordToFakeBinary(word);
            return Loc.GetString("accent-glitch-error");
        }

        private string AddStarsToWord(string word)
        {
            var accentedWord = new StringBuilder(word.Length);
            foreach (var letter in word)
            {
                if (_random.NextDouble() >= 0.8)
                    accentedWord.Append('*');
                else
                    accentedWord.Append(letter);
            }
            return accentedWord.ToString();
        }

        private string ChangeWordToFakeBinary(string word)
        {
            var accentedWord = new StringBuilder(word.Length);
            foreach (var _ in word)
            {
                if (_random.NextDouble() >= 0.5)
                    accentedWord.Append('0');
                else
                    accentedWord.Append('1');
            }
            return accentedWord.ToString();
        }

        private void OnAccent(EntityUid uid, GlitchAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
