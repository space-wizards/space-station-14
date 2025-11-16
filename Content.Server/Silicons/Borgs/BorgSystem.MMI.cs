using Content.Server.Ghost.Roles.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
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

    private void OnMMIInit(Entity<MMIComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<ItemSlotsComponent>(entity.Owner, out var itemSlots))
            return;

        if (ItemSlots.TryGetSlot(entity.Owner, entity.Comp.BrainSlotId, out var slot, itemSlots))
            entity.Comp.BrainSlot = slot;
        else
            ItemSlots.AddItemSlot(entity.Owner, entity.Comp.BrainSlotId, entity.Comp.BrainSlot, itemSlots);
    }

    private void OnMMIEntityInserted(Entity<MMIComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != entity.Comp.BrainSlotId)
            return;

        var ent = args.Entity;
        var linked = EnsureComp<MMILinkedComponent>(ent);
        linked.LinkedMMI = entity.Owner;
        Dirty(entity.Owner, entity.Comp);

        if (_mind.TryGetMind(ent, out var mindId, out var mind))
        {
            _mind.TransferTo(mindId, entity.Owner, true, mind: mind);

            if (!_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindAddRole(mindId, "MindRoleSiliconBrain", silent: true);
        }
        else if (entity.Comp.EnableGhostRole)
        {
            EnableGhostRole((entity.Owner, entity.Comp));
        }

        _appearance.SetData(entity.Owner, MMIVisuals.BrainPresent, true);
    }

    private void OnMMIMindAdded(Entity<MMIComponent> entity, ref MindAddedMessage args)
    {
        _appearance.SetData(entity.Owner, MMIVisuals.MindState, MMIVisualsMindstate.HasMind);
    }

    private void OnMMIMindRemoved(Entity<MMIComponent> entity, ref MindRemovedMessage args)
    {
        if (entity.Comp.EnableGhostRole)
            EnableGhostRole((entity.Owner, entity.Comp));
        else
            _appearance.SetData(entity.Owner, MMIVisuals.MindState, MMIVisualsMindstate.NoMind);
    }

    private void OnMMILinkedMindAdded(Entity<MMILinkedComponent> entity, ref MindAddedMessage args)
    {
        if (!_mind.TryGetMind(entity.Owner, out var mindId, out var mind) ||
            entity.Comp.LinkedMMI == null)
            return;

        _mind.TransferTo(mindId, entity.Comp.LinkedMMI, true, mind: mind);
    }

    private void OnMMILinkedRemoved(Entity<MMILinkedComponent> entity, ref EntGotRemovedFromContainerMessage args)
    {
        if (Terminating(entity.Owner))
            return;

        if (entity.Comp.LinkedMMI is not { } linked)
            return;
        RemComp(entity.Owner, entity.Comp);

        if (_mind.TryGetMind(linked, out var mindId, out var mind))
        {
            if (_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindRemoveRole<SiliconBrainRoleComponent>(mindId);

            _mind.TransferTo(mindId, entity.Owner, true, mind: mind);
        }
        else if (HasComp<GhostTakeoverAvailableComponent>(linked))
        {
            RemComp<GhostRoleComponent>(linked);
        }

        _appearance.SetData(linked, MMIVisuals.BrainPresent, false);
    }

    private void EnableGhostRole(Entity<MMIComponent> entity)
    {
        var ghostRole = EnsureComp<GhostRoleComponent>(entity.Owner);
        EnsureComp<GhostTakeoverAvailableComponent>(entity.Owner);

        //GhostRoleComponent inherits custom settings from the the MMI component
        ghostRole.RoleName = Loc.GetString(entity.Comp.RoleName);
        ghostRole.RoleDescription = Loc.GetString(entity.Comp.RoleDescription);
        ghostRole.RoleRules = Loc.GetString(entity.Comp.RoleRules);
        ghostRole.MindRoles = entity.Comp.MindRoles;
        ghostRole.JobProto = entity.Comp.JobProto;

        _appearance.SetData(entity.Owner, MMIVisuals.MindState, MMIVisualsMindstate.Searching);
    }
}
