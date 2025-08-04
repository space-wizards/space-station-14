using System.Text;
using Content.Shared.Drunk;
using Content.Shared.Speech.Components.AccentComponents;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class SlurredAccentSystem : AccentSystem<SlurredAccentComponent>
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Slur chance scales with "drunkeness", which is just measured using the time remaining on the status effect.
    /// </summary>
    private float GetProbabilityScale(EntityUid uid)
    {
        if (!_statusEffectsSystem.TryGetTime(uid, SharedDrunkSystem.DrunkKey, out var time))
            return 0;

        var curTime = _timing.CurTime;
        var timeLeft = (float) (time.Value.Item2 - curTime).TotalSeconds;
        return Math.Clamp((timeLeft - 80) / 1100, 0f, 1f);
    }

    public override string Accentuate(Entity<SlurredAccentComponent>? entity, string message)
    {
        var sb = new StringBuilder();

        var scale = entity != null ? GetProbabilityScale(entity.Value) : 0.5f;

        // This is pretty much ported from TG.
        foreach (var character in message)
        {
            if (_random.Prob(scale / 3f))
            {
                var lower = char.ToLowerInvariant(character);
                var newString = lower switch
                {
                    'o' => "u",
                    's' => "ch",
                    'a' => "ah",
                    'u' => "oo",
                    'c' => "k",
                    _ => $"{character}",
                };

                sb.Append(newString);
            }

            if (_random.Prob(scale / 20f))
            {
                if (character == ' ')
                {
                    sb.Append(Loc.GetString("slur-accent-confused"));
                }
                else if (character == '.')
                {
                    sb.Append(' ');
                    sb.Append(Loc.GetString("slur-accent-burp"));
                }
            }

            if (!_random.Prob(scale * 3/20))
            {
                sb.Append(character);
                continue;
            }

            var next = _random.Next(1, 3) switch
            {
                1 => "'",
                2 => $"{character}{character}",
                _ => $"{character}{character}{character}",
            };

            sb.Append(next);
        }

        return sb.ToString();
    }
}
