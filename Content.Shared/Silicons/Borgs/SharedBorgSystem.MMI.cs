using Content.Shared.Mind.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    public static readonly EntProtoId SiliconBrainRole = "MindRoleSiliconBrain";
    public static readonly EntProtoId SubvertedSiliconRole = "MindRoleSubvertedSilicon";

    public void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ComponentInit>(OnMMIInit);
        SubscribeLocalEvent<MMIComponent, EntInsertedIntoContainerMessage>(OnMMIEntityInserted);
        SubscribeLocalEvent<MMIComponent, MindAddedMessage>(OnMMIMindAdded);
        SubscribeLocalEvent<MMIComponent, MindRemovedMessage>(OnMMIMindRemoved);

        SubscribeLocalEvent<MMIComponent, EntRemovedFromContainerMessage>(OnMMILinkedRemoved);
    }

    private void OnMMIInit(Entity<MMIComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.BrainSlotId, ent.Comp.BrainSlot);
    }

    private void OnMMIEntityInserted(Entity<MMIComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        if (args.Container.ID != ent.Comp.BrainSlotId)
            return;

        var brain = args.Entity;

        if (_mind.TryGetMind(brain, out var mindId, out var mindComp))
        {
            // Mindroles are handled inside of SiliconLawSystem with MindAdded and MindRemoved messages.
            _mind.TransferTo(mindId, ent.Owner, true, mind: mindComp);
        }

        _appearance.SetData(ent.Owner, MMIVisuals.BrainPresent, true);
    }

    private void OnMMIMindAdded(Entity<MMIComponent> ent, ref MindAddedMessage args)
    {
        _appearance.SetData(ent.Owner, MMIVisuals.HasMind, true);
    }

    private void OnMMIMindRemoved(Entity<MMIComponent> ent, ref MindRemovedMessage args)
    {
        _appearance.SetData(ent.Owner, MMIVisuals.HasMind, false);
    }

    private void OnMMILinkedRemoved(Entity<MMIComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        if (args.Container.ID != ent.Comp.BrainSlotId)
            return;

        if (_mind.TryGetMind(ent, out var mindId, out var mindComp))
        {
            // Mindroles are handled inside of SiliconLawSystem with MindAdded and MindRemoved messages.
            _mind.TransferTo(mindId, args.Entity, true, mind: mindComp);
        }

        _appearance.SetData(ent, MMIVisuals.BrainPresent, false);
    }
}
