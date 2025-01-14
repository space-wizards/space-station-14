using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{

    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ComponentInit>(OnMMIInit);
        SubscribeLocalEvent<MMIComponent, EntInsertedIntoContainerMessage>(OnMMIEntityInserted);
        SubscribeLocalEvent<MMIComponent, MindAddedMessage>(OnMMIMindAdded);
        SubscribeLocalEvent<MMIComponent, MindRemovedMessage>(OnMMIMindRemoved);

        SubscribeLocalEvent<MMILinkedComponent, MindAddedMessage>(OnMMILinkedMindAdded);
        SubscribeLocalEvent<MMILinkedComponent, EntGotRemovedFromContainerMessage>(OnMMILinkedRemoved);
    }

    private void OnMMIInit(EntityUid uid, MMIComponent component, ComponentInit args)
    {
        if (!TryComp<ItemSlotsComponent>(uid, out var itemSlots))
            return;

        if (ItemSlots.TryGetSlot(uid, component.BrainSlotId, out var slot, itemSlots))
            component.BrainSlot = slot;
        else
            ItemSlots.AddItemSlot(uid, component.BrainSlotId, component.BrainSlot, itemSlots);
    }

    private void OnMMIEntityInserted(EntityUid uid, MMIComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.BrainSlotId)
            return;

        var ent = args.Entity;
        var linked = EnsureComp<MMILinkedComponent>(ent);
        linked.LinkedMMI = uid;
        Dirty(uid, component);

        _appearance.SetData(uid, MMIVisuals.BrainPresent, true);

        if (!_mind.TryGetMind(ent, out var mindId, out var mind))
            return;

        _mind.TransferTo(mindId, uid, true, mind: mind);

        if (!_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleSiliconBrain", silent: true);

        BorgJobSwitch((mindId, mind), linked);
    }

    private void OnMMIMindAdded(EntityUid uid, MMIComponent component, MindAddedMessage args)
    {
        _appearance.SetData(uid, MMIVisuals.HasMind, true);
    }

    private void OnMMIMindRemoved(EntityUid uid, MMIComponent component, MindRemovedMessage args)
    {
        _appearance.SetData(uid, MMIVisuals.HasMind, false);
    }

    private void OnMMILinkedMindAdded(EntityUid uid, MMILinkedComponent component, MindAddedMessage args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind) ||
            component.LinkedMMI == null)
            return;

        _mind.TransferTo(mindId, component.LinkedMMI, true, mind: mind);

        BorgJobSwitch((mindId, mind), component);
    }

    /// <summary>
    ///     Switches an MMI-d mind's job to borg and stores the previous value so it can be restored
    /// </summary>
    private void BorgJobSwitch(Entity<MindComponent> mind, MMILinkedComponent comp)
    {
        if(_roles.MindHasRole<JobRoleComponent>(mind.Owner, out var job))
            comp.OldJob = job.Value.Comp1.JobPrototype;

        _roles.MindAddJobRole(mind, mind, true, "Borg");
    }

    private void OnMMILinkedRemoved(EntityUid uid, MMILinkedComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (Terminating(uid))
            return;

        if (component.LinkedMMI is not { } linked)
            return;
        RemComp(uid, component);

        _appearance.SetData(linked, MMIVisuals.BrainPresent, false);

        if (!_mind.TryGetMind(linked, out var mindId, out var mind))
            return;

        if (_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
            _roles.MindRemoveRole<SiliconBrainRoleComponent>(mindId);

        _mind.TransferTo(mindId, uid, true, mind: mind);
        _roles.MindAddJobRole(mindId, mind, true, component.OldJob);
    }
}
