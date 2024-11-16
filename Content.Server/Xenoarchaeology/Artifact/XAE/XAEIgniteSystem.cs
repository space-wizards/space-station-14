using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAEIgniteSystem : BaseXAESystem<XAEIgniteComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEIgniteComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        var flammable = GetEntityQuery<FlammableComponent>();
        foreach (var target in _lookup.GetEntitiesInRange(ent.Owner, component.Range))
        {
            if (!flammable.TryGetComponent(target, out var fl))
                continue;

            fl.FireStacks += _random.Next(component.MinFireStack, component.MaxFireStack);
            _flammable.Ignite(target, ent.Owner, fl);
        }
    }
}
