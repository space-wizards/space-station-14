using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Damage;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class BreakWindowArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DamageNearbyArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, DamageNearbyArtifactComponent component, ArtifactActivatedEvent args)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.Radius))
        {
            if (component.Whitelist != null && !component.Whitelist.IsValid(ent))
                continue;

            _damageable.TryChangeDamage(ent, component.Damage, component.IgnoreResistances);
        }
    }
}
