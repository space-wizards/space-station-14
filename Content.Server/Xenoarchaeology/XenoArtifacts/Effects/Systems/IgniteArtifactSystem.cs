using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class IgniteArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<IgniteArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, IgniteArtifactComponent component, ArtifactActivatedEvent args)
    {
        var flammable = GetEntityQuery<FlammableComponent>();
        foreach (var target in _lookup.GetEntitiesInRange(uid, component.Range))
        {
            if (!flammable.TryGetComponent(target, out var fl))
                continue;
            fl.FireStacks += _random.Next(component.MinFireStack, component.MaxFireStack);
            _flammable.Ignite(target, uid, fl);
        }
    }
}
