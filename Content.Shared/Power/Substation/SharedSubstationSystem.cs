using Content.Shared.Containers.ItemSlots;
using Content.Shared.Wires;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared.Power.Substation;

public sealed partial class SharedSubstationSystem : EntitySystem
{

    [Dependency] private TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubstationFuseSlotComponent, EntInsertedIntoContainerMessage>(OnFuseInserted);
        SubscribeLocalEvent<SubstationFuseSlotComponent, EntRemovedFromContainerMessage>(OnFuseRemoved);
        SubscribeLocalEvent<SubstationFuseSlotComponent, ContainerIsInsertingAttemptEvent>(OnFuseInsertAttempt);
        SubscribeLocalEvent<SubstationFuseSlotComponent, ContainerIsRemovingAttemptEvent>(OnFuseRemoveAttempt);
        
    }

    private void OnFuseInsertAttempt(EntityUid uid, SubstationFuseSlotComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if(!component.Initialized)
            return;

        if(args.Container.ID != component.FuseSlotId)
            return;

        if(!TryComp<WiresPanelComponent>(uid, out var panel))
            return;

        if(!_tagSystem.HasTag(args.EntityUid, "Fuse"))
        {
            args.Cancel();
            return;
        }

        if(component.AllowInsert)
        {
            component.AllowInsert = false;
            return;
        }

        if(!panel.Open)
        {
            args.Cancel();
        }
        
    }

    private void OnFuseRemoveAttempt(EntityUid uid, SubstationFuseSlotComponent component, ContainerIsRemovingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.FuseSlotId)
            return;

        if(!TryComp<WiresPanelComponent>(uid, out var panel))
            return;
        
        if(!panel.Open)
        {
            args.Cancel();
        }
        
    }

    private void OnFuseInserted(EntityUid uid, SubstationFuseSlotComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.FuseSlotId)
            return;
        
        RaiseLocalEvent(uid, new SubstationFuseChangedEvent(), false);
    }

    private void OnFuseRemoved(EntityUid uid, SubstationFuseSlotComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.FuseSlotId)
            return;
        
        RaiseLocalEvent(uid, new SubstationFuseChangedEvent(), false);
    }
}
