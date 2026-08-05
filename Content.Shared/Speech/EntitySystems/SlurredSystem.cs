using System.Text;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SlurredSystem : RelayAccentSystem<SlurredAccentComponent>
{
    public static readonly EntProtoId Stutter = "StatusEffectSlurred";

    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override string Accentuate(string message, Entity<SlurredAccentComponent>? ent = null)
    {
        if (ent == null)
            return message;

        var scale = GetProbabilityScale(ent.Value);
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value));
        var sb = new StringBuilder();

        // This is pretty much ported from TG.
        foreach (var character in message)
        {
            if (random.Prob(scale / 3f))
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

            if (random.Prob(scale / 20f))
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

            if (!random.Prob(scale * 3 / 20))
            {
                sb.Append(character);
                continue;
            }

            var next = random.Next(1, 3) switch
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
    /// Slur chance scales with the time remaining on any status effect with the SlurredAccentComponent.
    /// Typically, this is equivalent to "drunkenness" on the DrunkStatusEffect
    /// </summary>
    private float GetProbabilityScale(Entity<SlurredAccentComponent> ent)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var component) || component.AppliedTo == null)
            return 1f;

        if (!_status.TryGetMaxTime<SlurredAccentComponent>(component.AppliedTo.Value, out var time))
            return 1f;

        // This is a magic number. Why this value? No clue it was made 3 years before I refactored this.
        var magic = time.Item2 == null
            ? ent.Comp.SlurredModifier
            : (float)(time.Item2 - _timing.CurTime).Value.TotalSeconds - ent.Comp.SlurredThreshold;

        return Math.Clamp(magic / ent.Comp.SlurredModifier, 0f, 1f);
    }
}
