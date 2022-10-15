using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public abstract class SharedSubdermalImplantSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public const string ImplantSlotId = "implantcontainer";
    public const string BaseStorageId = "storagebase";

    public override void Initialize()
    {
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SubdermalImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    private void OnInsert(EntityUid uid, SubdermalImplantComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (component.EntityUid == null)
            return;

        if (component.ImplantAction != null)
        {
            var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(component.ImplantAction));
            _actionsSystem.AddAction(component.EntityUid.Value, action, uid);
        }
    }

    private void OnRemoveAttempt(EntityUid uid, SubdermalImplantComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (component.Permanent && component.EntityUid != null)
            args.Cancel();
    }

    private void OnRemove(EntityUid uid, SubdermalImplantComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (component.EntityUid == null)
            return;

        var entCoords = Transform(component.EntityUid.Value).Coordinates;

        if (component.ImplantAction != null)
            _actionsSystem.RemoveProvidedActions(component.EntityUid.Value, uid);

        if (!_container.TryGetContainer(uid, BaseStorageId, out var storageImplant))
            return;

        _container.EmptyContainer(storageImplant, moveTo: entCoords);
    }

    /// <summary>
    /// Forces an implant into a person
    /// Good for on spawn related code or admin additions
    /// </summary>
    /// <param name="target">The entity to be implanted</param>
    /// <param name="implant"> The implant</param>
    /// <param name="component">The implant component</param>
    public void ForceImplant(EntityUid target, EntityUid implant, SubdermalImplantComponent component)
    {
        //If the target doesn't have the implanted component, add it.
        if (!HasComp<ImplantedComponent>(target))
            EnsureComp<ImplantedComponent>(target);

        var implantContainer = _container.EnsureContainer<Container>(target, ImplantSlotId);
        component.EntityUid = target;
        implantContainer.OccludesLight = false;
        implantContainer.Insert(implant);
    }

    /// <summary>
    /// Removes and deletes implants by force
    /// </summary>
    /// <param name="target">The entity to have implants removed</param>
    /// <param name="component">The implant component</param>
    public void ForceRemove(EntityUid target, SubdermalImplantComponent component)
    {
        if (!_container.TryGetContainer(target, ImplantSlotId, out var implantContainer))
            return;

        _container.CleanContainer(implantContainer);
    }
}
