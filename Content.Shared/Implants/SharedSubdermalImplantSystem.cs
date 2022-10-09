using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public abstract class SharedSubdermalImplantSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, GetItemActionsEvent>(GetImplantAction);
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);

        //TODO: Add trigger events and such for certain implant types
    }

    private void OnInsert(EntityUid uid, SubdermalImplantComponent component, EntGotInsertedIntoContainerMessage args)
    {
        //TODO: Probably remove this/swap to partial

        if (component.EntityUid == null || !_inventorySystem.TryGetSlot(component.EntityUid.Value, args.Container.ID, out var slotdef))
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
