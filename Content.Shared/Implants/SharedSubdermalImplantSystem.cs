using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Implants.Components;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public abstract class SharedSubdermalImplantSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public const string BaseStorageId = "storagebase";

    public override void Initialize()
    {
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SubdermalImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    private void OnInsert(EntityUid uid, SubdermalImplantComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (component.ImplantedEntity == null)
            return;

        if (component.ImplantAction != null)
        {
            var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(component.ImplantAction));
            _actionsSystem.AddAction(component.ImplantedEntity.Value, action, uid);
        }

        //replace micro bomb with macro bomb
        if (_container.TryGetContainer(component.ImplantedEntity.Value, ImplanterComponent.ImplantSlotId, out var implantContainer) && _tag.HasTag(uid, "MacroBomb"))
        {
            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (_tag.HasTag(implant, "MicroBomb"))
                {
                    implantContainer.Remove(implant);
                    QueueDel(implant);
                }
            }
        }
    }

    private void OnRemoveAttempt(EntityUid uid, SubdermalImplantComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (component.Permanent && component.ImplantedEntity != null)
            args.Cancel();
    }

    private void OnRemove(EntityUid uid, SubdermalImplantComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (component.ImplantedEntity == null || Terminating(component.ImplantedEntity.Value))
            return;

        if (component.ImplantAction != null)
            _actionsSystem.RemoveProvidedActions(component.ImplantedEntity.Value, uid);

        if (!_container.TryGetContainer(uid, BaseStorageId, out var storageImplant))
            return;

        var entCoords = Transform(component.ImplantedEntity.Value).Coordinates;

        var containedEntites = storageImplant.ContainedEntities.ToArray();

        foreach (var entity in containedEntites)
        {
            if (Terminating(entity))
                continue;

            _container.RemoveEntity(storageImplant.Owner, entity, force: true, destination: entCoords);
        }
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
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        component.ImplantedEntity = target;
        implantContainer.Insert(implant);
    }

    /// <summary>
    /// Force remove a singular implant
    /// </summary>
    /// <param name="target">the implanted entity</param>
    /// <param name="implant">the implant</param>
    /// <param name="component">the implant component</param>
    public void ForceRemove(EntityUid target, EntityUid implant)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return;

        var implantContainer = implanted.ImplantContainer;

        implantContainer.Remove(implant);
        QueueDel(implant);
    }

    /// <summary>
    /// Removes and deletes implants by force
    /// </summary>
    /// <param name="target">The entity to have implants removed</param>
    public void WipeImplants(EntityUid target)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return;

        var implantContainer = implanted.ImplantContainer;

        _container.CleanContainer(implantContainer);
    }
}
