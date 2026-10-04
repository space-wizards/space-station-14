using Content.Shared.Body;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Body;

public sealed partial class DetachableOrganSystem : EntitySystem
{
    [Dependency] private EntityQuery<DetachableOrganComponent> _detachableOrgan;
    [Dependency] private EntityQuery<OrganComponent> _organ;
    [Dependency] private OrganRelationSystem _organRelation = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    /// <summary>
    /// Detaches an organ from its containing body.
    /// </summary>
    /// <param name="organ">The organ to detach</param>
    /// <returns>The body that spawned when this organ was detached</returns>
    [PublicAPI]
    public EntityUid? Detach(Entity<DetachableOrganComponent?> organ)
    {
        if (!_detachableOrgan.Resolve(organ, ref organ.Comp) || !_organ.TryComp(organ, out var organComp) || organComp.Body is not { } oldBody)
            return null;

        _organRelation.Orphan(organ.Owner);
        var body = PredictedSpawnNextToOrDrop(organ.Comp.DetachedBody, oldBody);

        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var container))
        {
            Log.Error($"Entity {ToPrettyString(body)} relied on by {nameof(DetachableOrganComponent)} on {ToPrettyString(organ)} is missing a container ({BodyComponent.ContainerID}).");
            Del(body);
            return null;
        }

        if (!_container.Insert(organ.Owner, container, force: true))
        {
            Log.Error($"{ToPrettyString(organ)} could not be transferred to new body {ToPrettyString(body)}.");
        }

        foreach (var child in _organRelation.AllChildren(organ.Owner))
        {
            if (!_container.Insert(child.Owner, container, force: true))
            {
                Log.Error($"{ToPrettyString(child)} could not be transferred to new body {ToPrettyString(body)}.");
                _organRelation.Orphan(child.AsNullable());
            }
        }

        return body;
    }
}
