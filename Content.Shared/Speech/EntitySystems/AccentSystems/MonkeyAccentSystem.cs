using System.Text;
using Content.Shared.Speech.Components.AccentComponents;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class MonkeyAccentSystem : AccentSystem<MonkeyAccentComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Accentuate(Entity<MonkeyAccentComponent>? entity, string message)
    {
        var words = message.Split();
        var accentedMessage = new StringBuilder(message.Length + 2);

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];

            if (_random.NextDouble() >= 0.5)
            {
                if (word.Length > 1)
                {
                    foreach (var _ in word)
                    {
                        accentedMessage.Append('O');
                    }

                    if (_random.NextDouble() >= 0.3)
                        accentedMessage.Append('K');
                }
                else
                    accentedMessage.Append('O');
            }
            else
            {
                foreach (var _ in word)
                {
                    if (_random.NextDouble() >= 0.8)
                        accentedMessage.Append('H');
                    else
                        accentedMessage.Append('A');
                }

            }

            if (i < words.Length - 1)
                accentedMessage.Append(' ');
        }

        accentedMessage.Append('!');

        return accentedMessage.ToString();
    }
}
