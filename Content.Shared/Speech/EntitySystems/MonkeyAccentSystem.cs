using System.Text;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class MonkeyAccentSystem : RelayAccentSystem<MonkeyAccentComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override string Accentuate(string message, Entity<MonkeyAccentComponent>? ent = null)
    {
        var random = ent.HasValue
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;

        var words = message.Split();
        var accentedMessage = new StringBuilder(message.Length + 2);

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];

            if (random.NextDouble() >= 0.5)
            {
                if (word.Length > 1)
                {
                    foreach (var __ in word)
                    {
                        accentedMessage.Append('O');
                    }

                    if (random.NextDouble() >= 0.3)
                        accentedMessage.Append('K');
                }
                else
                    accentedMessage.Append('O');
            }
            else
            {
                foreach (var __ in word)
                {
                    if (random.NextDouble() >= 0.8)
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
