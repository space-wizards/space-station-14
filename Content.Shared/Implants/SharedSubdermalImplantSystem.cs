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
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, GetItemActionsEvent>(GetImplantAction);
        SubscribeLocalEvent<SubdermalImplantComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt); //replace with engine PR GotInserted
        SubscribeLocalEvent<SubdermalImplantComponent, GotInsertedEvent>(OnInsert);
        //TODO: Subscribe to GotInsertedEvent, which will be an engine PR for containers that's raised on the inserted entity
    }

    private void OnInsert(EntityUid uid, SubdermalImplantComponent component, GotInsertedEvent args)
    {
        if (component.EntityUid == null)
            return;

        if (component.ImplantAction != null)
        {
            var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(component.ImplantAction));
            _actionsSystem.AddAction(component.EntityUid.Value, action, uid);
        }
    }

    private void OnInsertAttempt(EntityUid uid, SubdermalImplantComponent component, ContainerGettingInsertedAttemptEvent args)
    {

    }

    private void GetImplantAction(EntityUid uid, SubdermalImplantComponent component, GetItemActionsEvent args)
    {
        //TODO: Determine if this can work since you need to add it on implant, rather than on pickup
        //TODO: Something like implant > check this component > add action instead of from here
    }
}
