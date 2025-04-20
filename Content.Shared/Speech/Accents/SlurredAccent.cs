using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Drunk;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class SlurredAccent : IAccent
{
    public string Name { get; } = "Slurred";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private StatusEffectsSystem _statusEffectsSystem = default!;

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);

        var scale = 0f;
        if (attributes.TryGetValue("chance", out var chanceParameter))
            scale = chanceParameter.LongValue!.Value;

        var sb = new StringBuilder();

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

    /// <summary>
    ///     Slur chance scales with "drunkeness", which is just measured using the time remaining on the status effect.
    /// </summary>
    private float GetProbabilityScale(EntityUid uid)
    {
        IoCManager.InjectDependencies(this);
        _statusEffectsSystem = _entSys.GetEntitySystem<StatusEffectsSystem>();

        if (!_statusEffectsSystem.TryGetTime(uid, SharedDrunkSystem.DrunkKey, out var time))
            return 0;

        var curTime = _timing.CurTime;
        var timeLeft = (float) (time.Value.Item2 - curTime).TotalSeconds;
        return Math.Clamp((timeLeft - 80) / 1100, 0f, 1f);
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        if (c is SlurredAccentComponent comp)
        {
            var scale = GetProbabilityScale(ev.Entity);
            ev.Accents.Add(
                Name,
                new Dictionary<string, MarkupParameter>() { { "scale", new MarkupParameter((long)scale) } });
        }
    }
}
