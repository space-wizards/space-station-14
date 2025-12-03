using Content.Shared.Mind.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    private static readonly EntProtoId SiliconBrainRole = "MindRoleSiliconBrain";

    public void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ComponentInit>(OnMMIInit);
        SubscribeLocalEvent<MMIComponent, EntInsertedIntoContainerMessage>(OnMMIEntityInserted);
        SubscribeLocalEvent<MMIComponent, MindAddedMessage>(OnMMIMindAdded);
        SubscribeLocalEvent<MMIComponent, MindRemovedMessage>(OnMMIMindRemoved);

        SubscribeLocalEvent<MMILinkedComponent, MindAddedMessage>(OnMMILinkedMindAdded);
        SubscribeLocalEvent<MMILinkedComponent, EntGotRemovedFromContainerMessage>(OnMMILinkedRemoved);
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
        var linked = EnsureComp<MMILinkedComponent>(brain);
        linked.LinkedMMI = ent.Owner;
        Dirty(brain, linked);

        if (_mind.TryGetMind(brain, out var mindId, out var mindComp))
        {
            _mind.TransferTo(mindId, ent.Owner, true, mind: mindComp);

            if (!_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindAddRole(mindId, SiliconBrainRole, silent: true);
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

    private void OnMMILinkedMindAdded(Entity<MMILinkedComponent> ent, ref MindAddedMessage args)
    {
        if (ent.Comp.LinkedMMI == null || !_mind.TryGetMind(ent.Owner, out var mindId, out var mindComp))
            return;

        _mind.TransferTo(mindId, ent.Comp.LinkedMMI, true, mind: mindComp);
    }

    private void OnMMILinkedRemoved(Entity<MMILinkedComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        if (Terminating(ent.Owner))
            return;

        if (ent.Comp.LinkedMMI is not { } linked)
            return;

        RemCompDeferred<MMILinkedComponent>(ent.Owner);

        if (_mind.TryGetMind(linked, out var mindId, out var mindComp))
        {
            if (_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindRemoveRole<SiliconBrainRoleComponent>(mindId);

            _mind.TransferTo(mindId, ent.Owner, true, mind: mindComp);
        }

        _appearance.SetData(linked, MMIVisuals.BrainPresent, false);
    }
}
