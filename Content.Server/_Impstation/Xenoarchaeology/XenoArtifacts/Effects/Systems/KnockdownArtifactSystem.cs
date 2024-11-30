using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Buckle.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Maps;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;


namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class KnockdownArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StunSystem _stuns = default!;

    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<StatusEffectsComponent> _statusQuery;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KnockdownArtifactComponent, ArtifactActivatedEvent>(OnActivated);
        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _statusQuery = GetEntityQuery<StatusEffectsComponent>();

    }

    private void OnActivated(EntityUid uid, KnockdownArtifactComponent component, ArtifactActivatedEvent args)
    {
        var transform = Transform(uid);
        var gridUid = transform.GridUid;
        // knock over everyone on the same grid, fall back to range if not on a grid.
        if (component.EntireGrid && gridUid != null) {
            var gridTransform = Transform((EntityUid)gridUid);
            var childEnumerator = gridTransform.ChildEnumerator;
            while (childEnumerator.MoveNext(out var child))
            {
                if (!_buckleQuery.TryGetComponent(child, out var buckle) || buckle.Buckled)
                    continue;

                if (!_statusQuery.TryGetComponent(child, out var status))
                    continue;

                _stuns.TryParalyze(child, TimeSpan.FromSeconds(component.KnockdownTime), true, status);
            }



        }
        else // knock over only people in range
        {
            var ents = _lookup.GetEntitiesInRange(uid, component.Range);
            if (args.Activator != null)
                ents.Add(args.Activator.Value);
            foreach (var ent in ents)
            {
                if (!_buckleQuery.TryGetComponent(ent, out var buckle) || buckle.Buckled)
                    continue;

                if (!_statusQuery.TryGetComponent(ent, out var status))
                    continue;

                _stuns.TryParalyze(ent, TimeSpan.FromSeconds(component.KnockdownTime), true, status);
            }

        }
    }
}
