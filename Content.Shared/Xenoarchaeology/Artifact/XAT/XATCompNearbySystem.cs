using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public sealed class XATCompNearbySystem : BaseXATSystem<XATCompNearbyComponent>
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<IComponent>> _entities = new();

    protected override void UpdateXAT(
        Entity<XenoArtifactComponent> artifact,
        Entity<XATCompNearbyComponent, XenoArtifactNodeComponent> node,
        float frameTime
    )
    {
        var pos = _transform.GetMapCoordinates(artifact);
        var comp = EntityManager.ComponentFactory.GetRegistration(node.Comp1.Comp);

        _entities.Clear();
        _entityLookup.GetEntitiesInRange(comp.Type, pos, node.Comp1.Radius, _entities);

        if (_entities.Count >= node.Comp1.Count)
            Trigger(artifact, node);
    }
}
