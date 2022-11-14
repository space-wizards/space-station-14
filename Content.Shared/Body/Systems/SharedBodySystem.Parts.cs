using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeParts()
    {
        SubscribeLocalEvent<BodyPartComponent, ComponentRemove>(OnPartRemoved);
        SubscribeLocalEvent<BodyPartComponent, ComponentGetState>(OnPartGetState);
        SubscribeLocalEvent<BodyPartComponent, ComponentHandleState>(OnPartHandleState);
    }

    private void OnPartGetState(EntityUid uid, BodyPartComponent part, ref ComponentGetState args)
    {
        args.State = new BodyPartComponentState(
            part.Body,
            part.ParentSlot,
            part.Children,
            part.Organs,
            part.PartType,
            part.IsVital,
            part.Symmetry
        );
    }

    private void OnPartHandleState(EntityUid uid, BodyPartComponent part, ref ComponentHandleState args)
    {
        if (args.Current is not BodyPartComponentState state)
            return;

        part.Body = state.Body;
        part.ParentSlot = state.ParentSlot;
        part.Children = state.Children;
        part.Organs = state.Organs;
        part.PartType = state.PartType;
        part.IsVital = state.IsVital;
        part.Symmetry = state.Symmetry;
    }

    private void OnPartRemoved(EntityUid uid, BodyPartComponent part, ComponentRemove args)
    {
        if (part.ParentSlot is { } slot)
        {
            slot.Child = null;
            Dirty(slot.Parent);
        }

        foreach (var childSlot in part.Children.Values.ToArray())
        {
            DropPart(childSlot.Child);
        }
    }

    private BodyPartSlot? CreatePartSlot(
        string slotId,
        EntityUid parent,
        BodyPartType partType,
        BodyPartComponent? part = null)
    {
        if (!Resolve(parent, ref part, false))
            return null;

        var slot = new BodyPartSlot(slotId, parent, partType);
        part.Children.Add(slotId, slot);

        return slot;
    }

    public bool TryCreatePartSlot(
        EntityUid? parentId,
        string id,
        [NotNullWhen(true)] out BodyPartSlot? slot,
        BodyPartComponent? parent = null)
    {
        slot = null;

        if (parentId == null ||
            !Resolve(parentId.Value, ref parent, false))
            return false;

        slot = new BodyPartSlot(id, parentId.Value, null);
        if (!parent.Children.TryAdd(id, slot))
        {
            slot = null;
            return false;
        }

        return true;
    }

    public bool TryCreatePartSlotAndAttach(
        EntityUid? parentId,
        string id,
        EntityUid? childId,
        BodyPartComponent? parent = null,
        BodyPartComponent? child = null)
    {
        return TryCreatePartSlot(parentId, id, out var slot, parent) && AttachPart(childId, slot, child);
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetPartChildren(EntityUid? id, BodyPartComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            yield break;

        foreach (var slot in part.Children.Values)
        {
            if (!TryComp(slot.Child, out BodyPartComponent? childPart))
                continue;

            yield return (slot.Child.Value, childPart);

            foreach (var subChild in GetPartChildren(slot.Child, childPart))
            {
                yield return subChild;
            }
        }
    }

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetPartOrgans(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false))
            yield break;

        foreach (var slot in part.Organs.Values)
        {
            if (!TryComp(slot.Child, out OrganComponent? organ))
                continue;

            yield return (slot.Child.Value, organ);
        }
    }

    public IEnumerable<BodyPartSlot> GetPartAllSlots(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false))
            yield break;

        foreach (var slot in part.Children.Values)
        {
            yield return slot;

            if (!TryComp(slot.Child, out BodyComponent? childPart))
                continue;

            foreach (var subChild in GetBodyAllSlots(slot.Child, childPart))
            {
                yield return subChild;
            }
        }
    }

    public bool CanAttachPart([NotNullWhen(true)] EntityUid? partId, BodyPartSlot slot, BodyPartComponent? part = null)
    {
        return partId != null &&
               slot.Child == null &&
               Resolve(partId.Value, ref part, false) &&
               (slot.Type == null || slot.Type == part.PartType) &&
               Containers.TryGetContainer(slot.Parent, BodyContainerId, out var container) &&
               container.CanInsert(partId.Value);
    }

    public virtual bool AttachPart(
        EntityUid? partId,
        BodyPartSlot slot,
        [NotNullWhen(true)] BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false) ||
            !CanAttachPart(partId, slot, part))
            return false;

        DropPart(slot.Child);
        DropPart(partId, part);

        var container = Containers.EnsureContainer<Container>(slot.Parent, BodyContainerId);
        if (!container.Insert(partId.Value))
            return false;

        slot.Child = partId;
        part.ParentSlot = slot;

        if (TryComp(slot.Parent, out BodyPartComponent? parentPart))
        {
            part.Body = parentPart.Body;
        }
        else if (TryComp(slot.Parent, out BodyComponent? parentBody))
        {
            part.Body = parentBody.Owner;
        }
        else
        {
            part.Body = null;
        }

        Dirty(slot.Parent);
        Dirty(partId.Value);

        if (part.Body is { } newBody)
        {
            var partAddedEvent = new BodyPartAddedEvent(slot.Id, part);
            RaiseLocalEvent(newBody, ref partAddedEvent);

            // TODO: Body refactor. Somebody is doing it
            // EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartAdded(Owner, argsAdded);

            foreach (var organ in GetPartOrgans(partId, part))
            {
                RaiseLocalEvent(organ.Id, new AddedToBodyEvent(newBody), true);
            }

            Dirty(newBody);
        }

        return true;
    }

    public virtual bool DropPart(EntityUid? partId, [NotNullWhen(true)] BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false) ||
            part.ParentSlot is not { } slot)
            return false;

        var oldBodyNullable = part.Body;

        slot.Child = null;
        part.ParentSlot = null;
        part.Body = null;

        if (Containers.TryGetContainer(slot.Parent, BodyContainerId, out var container))
            container.Remove(partId.Value);

        if (TryComp(partId, out TransformComponent? transform))
            transform.AttachToGridOrMap();

        part.Owner.RandomOffset(0.25f);

        if (oldBodyNullable is { } oldBody)
        {
            var args = new BodyPartRemovedEvent(slot.Id, part);
            RaiseLocalEvent(oldBody, ref args);

            if (part.PartType == BodyPartType.Leg &&
                !GetBodyChildrenOfType(oldBody, BodyPartType.Leg).Any())
            {
                Standing.Down(oldBody);
            }

            if (part.IsVital && !GetBodyChildrenOfType(oldBody, part.PartType).Any())
            {
                // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
                var damage = new DamageSpecifier(Prototypes.Index<DamageTypePrototype>("Bloodloss"), 300);
                Damageable.TryChangeDamage(part.Owner, damage);
            }

            foreach (var organSlot in part.Organs.Values)
            {
                if (organSlot.Child is not { } child)
                    continue;

                RaiseLocalEvent(child, new RemovedFromBodyEvent(oldBody), true);
            }
        }

        Dirty(slot.Parent);
        Dirty(partId.Value);

        return true;
    }

    public bool DropPartAt(EntityUid? partId, EntityCoordinates dropAt, BodyPartComponent? part = null)
    {
        if (partId == null || !DropPart(partId, part))
            return false;

        if (TryComp(partId.Value, out TransformComponent? transform))
            transform.Coordinates = dropAt;

        return true;
    }

    public bool OrphanPart(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false))
            return false;

        DropPart(partId, part);

        foreach (var slot in part.Children.Values)
        {
            DropPart(slot.Child);
        }

        return false;
    }

    public bool DeletePart(EntityUid? id, BodyPartComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            return false;

        DropPart(id, part);

        if (Deleted(id.Value))
            return false;

        Del(id.Value);
        return true;
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildrenOfType(EntityUid? bodyId, BodyPartType type, BodyComponent? body = null)
    {
        foreach (var part in GetBodyChildren(bodyId, body))
        {
            if (part.Component.PartType == type)
                yield return part;
        }
    }

    public bool BodyHasChildOfType(EntityUid? bodyId, BodyPartType type, BodyComponent? body = null)
    {
        return GetBodyChildrenOfType(bodyId, type, body).Any();
    }

    public bool BodyHasChild(
        EntityUid? parentId,
        EntityUid? childId,
        BodyComponent? parent = null,
        BodyPartComponent? child = null)
    {
        if (parentId == null ||
            !Resolve(parentId.Value, ref parent, false) ||
            childId == null ||
            !Resolve(childId.Value, ref child, false))
            return false;

        return child.ParentSlot?.Child == parentId;
    }
}
