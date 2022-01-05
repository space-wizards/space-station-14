using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Systems;

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

    public BodyPartSlot SetSlot(EntityUid uid, string id, BodyPartType type, SharedBodyComponent body)
    {
        var slot = new BodyPartSlot(id, type);

        body.SlotIds[id] = slot;
        return slot;
    }

    #endregion

    #region Adding Parts

    public virtual bool CanAddPart(EntityUid uid, string slotId, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (!body.SlotIds.TryGetValue(slotId, out var slot))
            return false;

        if (part.PartType != slot.PartType)
            return false;

        return true;
    }

    public bool TryAddPart(EntityUid uid, string slotId, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (!CanAddPart(uid, slotId, part))
            return false;

        return AddPart(uid, slotId, part, body);
    }

    /// <summary>
    ///     Sets a slot ID's part to the given part.
    ///     Removes the existing part from the slot, if there is one.
    /// </summary>
    public bool AddPart(EntityUid uid, string slotId, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
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
        part.Body = body;
        body.PartContainer.Insert(part.Owner);

        OnPartAdded(uid, slot, part, body);

        return true;
    }


    protected virtual void OnPartAdded(EntityUid uid, BodyPartSlot slot, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return;

        var argsAdded = new BodyPartAddedEventArgs(slot.Id, part);

        _humanoidAppearanceSystem.BodyPartAdded(uid, argsAdded);
        foreach (var component in EntityManager.GetComponents<IBodyPartAdded>(uid).ToArray())
        {
            component.BodyPartAdded(argsAdded);
        }

        // TODO BODY Sort this duplicate out
        body.Dirty();
    }

    #endregion

    #region Removing Parts

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

        slot.Part = null;
        body.Parts.Remove(old);
        body.PartContainer.Remove(old.Owner);
        old.Body = null;

        foreach (var part in GetHangingParts(uid, slot, body))
        {
            RemovePart(uid, part.Value, body);
        }

        OnPartRemoved(uid, slot, old, body);

        return true;
    }

    /// <summary>
    ///     Removes the given part from the body. Defaults to finding the slot and removing the part that way.
    /// </summary>
    public bool RemovePart(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (body.Parts.TryGetValue(part, out var slot))
        {
            return RemovePart(uid, slot, body);
        }

        return false;
    }

    public bool TryRemovePart(EntityUid uid, BodyPartSlot slot, [NotNullWhen(true)] out Dictionary<BodyPartSlot, SharedBodyPartComponent>? dropped,
        SharedBodyComponent? body=null)
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

    protected virtual void OnPartRemoved(EntityUid uid, BodyPartSlot slot, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return;

        var args = new BodyPartRemovedEventArgs(slot.Id, part);

        // TODO MIRROR REMOVE make this a fucking event oh my god
        _humanoidAppearanceSystem.BodyPartRemoved(uid, args);
        foreach (var component in EntityManager.GetComponents<IBodyPartRemoved>(uid))
        {
            component.BodyPartRemoved(args);
        }

        // TODO MIRROR this should also be an event
        // creadth: fall down if no legs
        if (part.PartType == BodyPartType.Leg &&
            GetPartsOfType(uid, BodyPartType.Leg, body).ToArray().Length == 0)
        {
            _standingStateSystem.Down(uid);
        }

        // TODO MIRROR this should also be an event
        if (part.IsVital && body.Parts.Count(x => x.Value.PartType == part.PartType) == 0)
        {
            // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
            var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Bloodloss"), 300);
            _damageableSystem.TryChangeDamage(part.Owner, damage);
        }

        body.Dirty();
    }

    #endregion
}
