using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Chemistry.Components;
using Content.Server.Popups;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Containers;
using Robust.Server.Audio;
using Content.Shared.Coordinates;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    public void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ComponentInit>(OnMMIInit);
        SubscribeLocalEvent<MMIComponent, EntInsertedIntoContainerMessage>(OnMMIEntityInserted);
        SubscribeLocalEvent<MMIComponent, MindAddedMessage>(OnMMIMindAdded);
        SubscribeLocalEvent<MMIComponent, MindRemovedMessage>(OnMMIMindRemoved);
        SubscribeLocalEvent<MMIComponent, ItemSlotInsertAttemptEvent>(OnMMIAttemptInsert);

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

        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _mind.TransferTo(mindId, uid, true, mind: mind);

        _appearance.SetData(uid, MMIVisuals.BrainPresent, true);
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
    }

    private void OnMMILinkedRemoved(EntityUid uid, MMILinkedComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (Terminating(uid))
            return;

        if (component.LinkedMMI is not { } linked)
            return;
        RemComp(uid, component);

        if (_mind.TryGetMind(linked, out var mindId, out var mind))
            _mind.TransferTo(mindId, uid, true, mind: mind);

        _appearance.SetData(linked, MMIVisuals.BrainPresent, false);
    }

    private void OnMMIAttemptInsert(EntityUid uid, MMIComponent component, ItemSlotInsertAttemptEvent args)
    {
        var ent = args.Item;
        if (HasComp<UnborgableComponent>(ent))
        {
            _popup.PopupEntity("The brain suddenly dissolves on contact with the interface!", uid, Shared.Popups.PopupType.MediumCaution);
            _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", uid);
            var solution = new Solution();
            solution.AddReagent("Blood", 30f);
            _puddle.TrySpillAt(Transform(uid).Coordinates, solution, out _);
            EntityManager.DeleteEntity(ent);
        }
    }
}
