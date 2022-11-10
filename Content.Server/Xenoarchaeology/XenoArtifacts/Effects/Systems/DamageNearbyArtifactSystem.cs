using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Damage;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class BreakWindowArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DamageNearbyArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, DamageNearbyArtifactComponent component, ArtifactActivatedEvent args)
    {
        var ents = _lookup.GetEntitiesInRange(uid, component.Radius);
        if (args.Activator != null)
            ents.Add(args.Activator.Value);
        foreach (var ent in ents)
        {
            if (component.Whitelist != null && !component.Whitelist.IsValid(ent))
                continue;

            if (!_random.Prob(component.DamageChance))
                return;

            _damageable.TryChangeDamage(ent, component.Damage, component.IgnoreResistances);
        }
    }
}
