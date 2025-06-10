using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that ignites any flammable entity in range.
/// </summary>
public sealed class XAEIgniteSystem : BaseXAESystem<XAEIgniteComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    private EntityQuery<FlammableComponent> _flammables;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<EntityUid> _entities = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _flammables = GetEntityQuery<FlammableComponent>();
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEIgniteComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        _entities.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, component.Range, _entities);
        foreach (var target in _entities)
        {
            if (!_flammables.TryGetComponent(target, out var fl))
                continue;

            fl.FireStacks += component.FireStack.Next(_random);
            _flammable.Ignite(target, ent.Owner, fl);
        }
    }
}
