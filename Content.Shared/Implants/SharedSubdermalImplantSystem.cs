using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Implants.Components;
using Content.Shared.MobState.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public abstract class SharedSubdermalImplantSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedMobStateSystem _mobStateSystem = default!;

    public const string ImplantSlotId = "ImplantContainer";

    public override void Initialize()
    {
        SubscribeLocalEvent<SubdermalImplantComponent, GetItemActionsEvent>(GetImplantAction);
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);

        //TODO: Add trigger events and such for certain implant types
    }

    private void OnInsert(EntityUid uid, SubdermalImplantComponent component, EntGotInsertedIntoContainerMessage args)
    {
        //TODO: Probably remove this/swap to partial

        if (component.EntityUid == null)
            return;

        if (component.ImplantAction != null)
        {
            var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(component.ImplantAction));
            _actionsSystem.AddAction(component.EntityUid.Value, action, uid);
        }

        //TODO: See if you need to put any passive or reactive implant logic in here

        //TODO: See about dynamically adding components based on some injections (IE Undead, Mindshield, etc)
    }

    private void GetImplantAction(EntityUid uid, SubdermalImplantComponent component, GetItemActionsEvent args)
    {
        //TODO: Determine if you need this at all
    }
}
