using Content.Shared.Body;
using Robust.Shared.Containers;

namespace Content.Shared._Offbrand.Skeletons;

public sealed class DetachableOrganSystem : EntitySystem
{
    [Dependency] private readonly EntityQuery<DetachableOrganComponent> _detachableOrgan = default!;
    [Dependency] private readonly OrganRelationSystem _organRelation = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetachableOrganComponent, OrganGotRemovedEvent>(OnDetachableRemoved);
    }

    private void OnDetachableRemoved(Entity<DetachableOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Target))
            return;

        foreach (var parent in _organRelation.AllParents(ent.Owner))
        {
            if (_detachableOrgan.TryGetComponent(parent, out var detachableParent) && detachableParent.Detaching)
                return;
        }

        ent.Comp.Detaching = true;

        _organRelation.Orphan(ent.Owner);
        var body = PredictedSpawnNextToOrDrop(ent.Comp.DetachedBody, ent);

        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var container))
        {
            Log.Error($"Entity {ToPrettyString(body)} relied on by {nameof(DetachableOrganComponent)} on {ToPrettyString(ent)} is missing a container ({BodyComponent.ContainerID}).");
            ent.Comp.Detaching = false;
            Del(body);
            return;
        }

        if (!_container.Insert(ent.Owner, container))
        {
            Log.Error($"{ToPrettyString(ent)} could not be transferred to new body {ToPrettyString(body)}.");
        }

        foreach (var child in _organRelation.AllChildren(ent.Owner))
        {
            if (!_container.Insert(child.Owner, container))
            {
                Log.Error($"{ToPrettyString(child)} could not be transferred to new body {ToPrettyString(body)}.");
                _organRelation.Orphan(child.AsNullable());
            }
        }

        ent.Comp.Detaching = false;
    }
}
