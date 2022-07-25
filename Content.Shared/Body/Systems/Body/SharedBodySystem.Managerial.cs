using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Containers;

namespace Content.Shared.Body.Systems.Body;

public abstract partial class SharedBodySystem
{
    public void InitializeManagerial()
    {
        SubscribeLocalEvent<SharedBodyComponent, ComponentInit>(OnComponentInit);
    }

    protected virtual void OnComponentInit(EntityUid uid, SharedBodyComponent component, ComponentInit args)
    {
        component.PartContainer = _containerSystem.EnsureContainer<Container>(uid, $"{component.Name}-Parts");

        UpdateFromTemplate(uid, component.TemplateId, component);
    }

    public void UpdateFromTemplate(EntityUid uid, string templateId,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return;

        var template = _prototypeManager.Index<BodyTemplatePrototype>(templateId);

        foreach (var (id, partType) in template.Slots)
        {
            SetSlot(uid, id, partType, body);
        }

        foreach (var (slotId, connectionIds) in template.Connections)
        {
            var connections = connectionIds.Select(id => body.SlotIds[id]);
            body.SlotIds[slotId].Connections = new HashSet<BodyPartSlot>(connections);
        }
    }

    #region Slots

    /// <summary>
    ///     Creates a new slot name for a given part.
    /// </summary>
    public string GenerateUniqueSlotName(SharedBodyPartComponent part)
    {
        // e.g. 8912-Arm-Left
        // Can't see how we'd get a collision from this.
        return $"{part.Owner.ToString()}-{part.PartType.ToString()}-{part.Compatibility.ToString()}";
    }

    public BodyPartSlot SetSlot(EntityUid uid, string id, BodyPartType type, SharedBodyComponent body)
    {
        var slot = new BodyPartSlot(id, type);

        body.SlotIds[id] = slot;
        return slot;
    }

    #endregion

    #region Adding Parts

    public bool CanAddPart(EntityUid uid, string slotId, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (!body.SlotIds.TryGetValue(slotId, out var slot))
            return false;

        if (part.PartType != slot.PartType)
            return false;

        return true;
    }

    /// <summary>
    ///     Tries to add a given part, generating a new slot name for it.
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

        if (!body.SlotIds.TryGetValue(slotId, out var slot))
        {
            slot = SetSlot(uid, slotId, part.PartType, body);
            body.SlotIds[slotId] = slot;
        }

        if (slot.Part != null)
        {
            RemovePart(uid, slot, body);
        }

        slot.Part = part;
        body.Parts[part] = slot;
        var ev = new PartAddedToBodyEvent(uid, part.Owner, slot.Id);
        part.Body = body;
        OnPartAdded(body, part);

        // Raise the event on both the body and the part.
        RaiseLocalEvent(uid, ev, true);
        RaiseLocalEvent(part.Owner, ev);
        RaiseNetworkEvent(ev);

        // TODO BODY Sort this duplicate out
        body.Dirty();

        return true;
    }

    /// <summary>
    ///     Server needs to do things like add the part to a container,
    ///     so we'll just have a virtual call.
    /// </summary>
    protected virtual void OnPartAdded(SharedBodyComponent body, SharedBodyPartComponent part) { }

    #endregion

    #region Removing Parts

    public bool TryRemovePart(EntityUid uid, BodyPartSlot slot, [NotNullWhen(true)] out Dictionary<BodyPartSlot, SharedBodyPartComponent>? dropped,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
        {
            dropped = null;
            return false;
        }

        if (!body.SlotIds.TryGetValue(slot.Id, out var ownedSlot) ||
            ownedSlot != slot ||
            slot.Part == null)
        {
            dropped = null;
            return false;
        }

        var oldPart = slot.Part;
        dropped = GetHangingParts(uid, slot, body);

        if (!RemovePart(uid, oldPart, body))
        {
            dropped = null;
            return false;
        }

        dropped[slot] = oldPart;
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

        if (body.Parts.TryGetValue(part, out var slot))
        {
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

        var ev = new PartRemovedFromBodyEvent(uid, old.Owner, slot.Id);
        slot.Part = null;
        body.Parts.Remove(old);

        OnPartRemoved(body, old);
        old.Body = null;

        foreach (var part in GetHangingParts(uid, slot, body))
        {
            RemovePart(uid, part.Value, body);
        }

        // Raise the event on both the body and the part.
        RaiseLocalEvent(uid, ev);
        RaiseLocalEvent(old.Owner, ev);
        RaiseNetworkEvent(ev);

        body.Dirty();

        return true;
    }

    protected virtual void OnPartRemoved(SharedBodyComponent body, SharedBodyPartComponent part) { }

    #endregion
}
