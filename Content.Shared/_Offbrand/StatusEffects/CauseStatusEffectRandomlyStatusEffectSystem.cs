using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class CauseStatusEffectRandomlyStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<StatusEffectComponent, CauseStatusEffectRandomlyStatusEffectComponent>();
        var statusEffectsToAdd = new List<(EntityUid, EntProtoId, TimeSpan)>();

        while (enumerator.MoveNext(out var uid, out var effect, out var randomEffects))
        {
            if (_timing.CurTime < randomEffects.NextUpdate || effect.AppliedTo is not { } target)
                continue;

            randomEffects.NextUpdate = _timing.CurTime + randomEffects.UpdateInterval;
            Dirty(uid, randomEffects);

            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(uid).Id });
            var rand = new System.Random(seed);

            if (!rand.Prob(randomEffects.Probability))
                continue;

            foreach (var (proto, duration) in randomEffects.Effects)
            {
                // work around a concurrent modification exception
                statusEffectsToAdd.Add((target, proto, duration));
            }
        }

        // work around a concurrent modification exception
        foreach (var (target, proto, duration) in statusEffectsToAdd)
        {
            _statusEffects.TryAddStatusEffectDuration(target, proto, out _, duration);
        }
    }
}
