using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires some entity/entities with certain component on them nearby.
/// </summary>
public sealed class XATCompNearbySystem : BaseQueryUpdateXATSystem<XATCompNearbyComponent>
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<Entity<IComponent>> _entities = new();

    /// <inheritdoc />
    protected override void UpdateXAT(
        Entity<XenoArtifactComponent> artifact,
        Entity<XATCompNearbyComponent, XenoArtifactNodeComponent> node,
        float frameTime
    )
    {
        var compNearbyComponent = node.Comp1;

        var pos = _transform.GetMapCoordinates(artifact);
        var comp = EntityManager.ComponentFactory.GetRegistration(compNearbyComponent.RequireComponentWithName);

        _entities.Clear();
        _entityLookup.GetEntitiesInRange(comp.Type, pos, compNearbyComponent.Radius, _entities);
        if (_entities.Count >= compNearbyComponent.Count)
            Trigger(artifact, node);
    }
}
