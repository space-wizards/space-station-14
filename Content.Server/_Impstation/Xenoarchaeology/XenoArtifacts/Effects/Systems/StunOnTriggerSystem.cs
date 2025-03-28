using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Buckle.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class StunOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StunSystem _stuns = default!;

    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<StatusEffectsComponent> _statusQuery;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StunOnTriggerComponent, TriggerEvent>(OnActivated);
        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _statusQuery = GetEntityQuery<StatusEffectsComponent>();

    }

    private void OnActivated(EntityUid uid, StunOnTriggerComponent component, TriggerEvent args)
    {
        var transform = Transform(uid);
        var gridUid = transform.GridUid;
        // knock over everyone on the same grid, fall back to range if not on a grid.
        if (component.EntireGrid && gridUid != null) {
            HashSet<Entity<StatusEffectsComponent>> entities = new();
            _lookup.GetGridEntities<StatusEffectsComponent>((EntityUid)gridUid, entities);
            foreach (var ent in entities)
            {
                if (_buckleQuery.TryGetComponent(ent, out var buckle))
                {
                    if (buckle.Buckled)
                        continue;
                }

                if (!_statusQuery.TryGetComponent(ent, out var status))
                    continue;

                _stuns.TryParalyze(ent, TimeSpan.FromSeconds(component.KnockdownTime), true, status);
            }

        }
        else // knock over only people in range
        {
            var ents = _lookup.GetEntitiesInRange(uid, component.Range);

            foreach (var ent in ents)
            {
                if (_buckleQuery.TryGetComponent(ent, out var buckle))
                {
                    if (buckle.Buckled)
                        continue;
                }

                if (!_statusQuery.TryGetComponent(ent, out var status))
                    continue;

                _stuns.TryParalyze(ent, TimeSpan.FromSeconds(component.KnockdownTime), true, status);
            }

        }
    }
}
