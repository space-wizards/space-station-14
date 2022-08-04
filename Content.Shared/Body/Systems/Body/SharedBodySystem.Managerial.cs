using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;

namespace Content.Shared.Body.Systems.Body;

public abstract partial class SharedBodySystem
{
    /// <summary>
    ///     Populates the BodyComponent with slots based provided BodyTemplate prototype ID
    /// </summary>
    public void UpdateFromTemplate(EntityUid uid, string templateId,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return;

        if (string.IsNullOrEmpty(templateId) ||
            !PrototypeManager.TryIndex<BodyTemplatePrototype>(templateId, out var template))
            return;

        foreach (var (id, partType) in template.Slots)
        {
            var slot = new BodyPartSlot(id, partType);
            if (id == template.CenterSlot)
                slot.IsCenterSlot = true;

            SetSlot(uid, id, slot, body);
        }

        foreach (var (slotId, connectionIds) in template.Connections)
        {
            SetSlotConnections(uid, slotId, connectionIds, body);
        }
    }

    /// <summary>
    ///     Sets a slot's connections with the provided connection IDs
    /// </summary>
    public void SetSlotConnections(EntityUid uid, string slotId, HashSet<string> connectionIds, SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return;

        if (!body.Slots.TryGetValue(slotId, out var slot))
            return;

        slot.Connections = new HashSet<string>(connectionIds);
    }

    #region Slots

    /// <summary>
    ///     Sets the provided body part slot, overwriting all data
    /// </summary>
    public void SetSlot(EntityUid uid, string slotId, BodyPartSlot slot, SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return;

        slot.ContainerSlot = ContainerSystem.EnsureContainer<ContainerSlot>(body.Owner, slotId);
        body.Slots[slotId] = slot;
        Dirty(body);
    }

    /// <summary>
    ///     Completely removes a body part slot from the body, dropping the contained part if possible
    /// </summary>
    public void RemoveSlot(EntityUid uid, BodyPartSlot slot, SharedBodyComponent? body = null)
    {
        if (slot.ContainerSlot != null)
            slot.ContainerSlot.Shutdown();

        if (!Resolve(uid, ref body, logMissing: false))
            return;

        var partRemoved = RemovePart(uid, slot, body);
        body.Slots.Remove(slot.Id);

        // if a part was removed the component was already dirtied
        if (!partRemoved)
            Dirty(body);
    }

    #endregion

    #region Adding Parts

    /// <summary>
    ///     Checks if a part can be added to the provided slot, does not return false if the slot already since by default the new part will replace the old part
    /// </summary>
    public bool CanAddPart(EntityUid uid, string slotId, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (!body.Slots.TryGetValue(slotId, out var slot))
            return false;

        if (part.PartType != slot.PartType)
            return false;

        if (!IsCompatible(uid, part, slot, body))
            return false;

        return slot.ContainerSlot?.CanInsertIfEmpty(part.Owner, EntityManager) ?? false;
    }

    /// <summary>
    ///     Tries to add a given part, generating a new slot for it.
    /// </summary>
    public bool TryAddPart(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        return TryAddPart(uid, GenerateUniqueSlotName(part), part, body);
    }

    /// <summary>
    ///     Tries to add a given part to a body in a given slot.
    /// </summary>
    public bool TryAddPart(EntityUid uid, string slotId, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (!CanAddPart(uid, slotId, part))
            return false;

        return AddPart(uid, slotId, part, body);
    }

    /// <summary>
    ///     Adds a given part, generating a new slot name for it.
    ///
    ///     Does not check if this can be done.
    /// </summary>
    public bool AddPart(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        return AddPart(uid, GenerateUniqueSlotName(part), part, body);
    }

    /// <summary>
    ///     Sets a slot ID's part to the given part.
    ///     Removes the existing part from the slot, if there is one.
    ///
    ///     Does not check if this can be done.
    /// </summary>
    public bool AddPart(EntityUid uid, string slotId, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (!body.Slots.TryGetValue(slotId, out var slot))
        {
            slot = new BodyPartSlot(slotId, part.PartType);
            SetSlot(uid, slotId, slot, body);

            body.Slots[slotId] = slot;
        }

        if (slot.Part != null && slot.Part != part.Owner &&
            !RemovePart(uid, slot, body))
            return false;

        return AddPartAndRaiseEvents(part.Owner, slot, body);
    }

    protected bool AddPartAndRaiseEvents(EntityUid part, BodyPartSlot slot, SharedBodyComponent body)
    {
        if (slot.ContainerSlot == null)
            return false;

        if (!slot.ContainerSlot.Insert(part))
            return false;

        var ev = new PartAddedToBodyEvent(body.Owner, part, slot.Id);
        RaiseLocalEvent(body.Owner, ev);
        RaiseLocalEvent(part, ev);

        return true;
    }

    #endregion

    #region Removing Parts

    /// <summary>
    /// Checks if a body part can be removed from the provided slot
    /// </summary>
    public bool CanRemove(EntityUid uid, BodyPartSlot slot, SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (slot.Part == null)
            return false;

        return slot.ContainerSlot?.CanRemove(slot.Part.Value, EntityManager) ?? false;
    }

    /// <summary>
    /// Tries to remove the body part in the provided slot
    /// </summary>
    /// <returns></returns>
    public bool TryRemovePart(EntityUid uid, BodyPartSlot slot, [NotNullWhen(true)] out Dictionary<BodyPartSlot, SharedBodyPartComponent>? dropped,
        SharedBodyComponent? body = null)
    {
        dropped = null;
        if (!Resolve(uid, ref body))
            return false;

        if (!CanRemove(uid, slot, body))
            return false;

        if (!body.Slots.TryGetValue(slot.Id, out var ownedSlot) ||
            ownedSlot != slot)
            return false;

        var oldPart = slot.Part;
        if (!RemovePart(uid, slot, body))
            return false;

        dropped = GetHangingParts(uid, slot, body);

        if (oldPart != null && TryComp<SharedBodyPartComponent>(oldPart.Value, out var comp))
            dropped[slot] = comp;

        return true;
    }

    /// <summary>
    ///     Removes the given part from the body. Defaults to finding the slot and removing the part that way.
    /// </summary>
    public bool RemovePart(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        foreach (var (_, slot) in body.Slots)
        {
            if (slot.HasPart && slot.Part == part.Owner)
                return RemovePart(uid, slot, body);
        }

        return false;
    }

    /// <summary>
    ///     Removes the part inside of a body part slot.
    /// </summary>
    public bool RemovePart(EntityUid uid, BodyPartSlot slot,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (slot.Part == null) return false;

        var old = slot.Part;

        if (!RemovePartAndRaiseEvents(old.Value, slot, body))
            return false;

        foreach (var hangingPart in GetHangingParts(uid, slot, body))
        {
            RemovePart(uid, hangingPart.Key, body);
        }

        return true;
    }

    protected bool RemovePartAndRaiseEvents(EntityUid part, BodyPartSlot slot, SharedBodyComponent body)
    {
        if (slot.ContainerSlot == null)
            return false;

        if (!slot.ContainerSlot.Remove(part))
            return false;

        part.RandomOffset(0.25f);

        var ev = new PartRemovedFromBodyEvent(body.Owner, part, slot.Id);
        RaiseLocalEvent(body.Owner, ev);
        RaiseLocalEvent(part, ev);

        return true;
    }

    #endregion
}
