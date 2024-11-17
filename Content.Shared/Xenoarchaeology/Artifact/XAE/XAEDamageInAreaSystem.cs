using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEDamageInAreaSystem : BaseXAESystem<XAEDamageInAreaComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEDamageInAreaComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var damageInAreaComponent = ent.Comp;
        var entitiesInRange = _lookup.GetEntitiesInRange(ent.Owner, damageInAreaComponent.Radius);

        foreach (var entityInRange in entitiesInRange)
        {
            if (_whitelistSystem.IsWhitelistFail(damageInAreaComponent.Whitelist, entityInRange))
                continue;

            if (!_random.Prob(damageInAreaComponent.DamageChance))
                return;

            _damageable.TryChangeDamage(entityInRange, damageInAreaComponent.Damage, damageInAreaComponent.IgnoreResistances);
        }
    }
}
