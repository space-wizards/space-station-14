using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Movement.Components;
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
    }

    private void OnPartRemoved(EntityUid uid, BodyPartComponent part, ComponentRemove args)
    {
        if (part.Parent is { } parent)
        {
            DetachPart(uid, part);
            DirtyAllComponents(parent);
        }
        //TODO: drop all parts otherwise the entities will desync from the sprites
        foreach (var (_,slot) in part.Children)
        {
            DropPart(slot.Entity);
        }
    }

    private ContainerSlot? CreatePartSlot(
        EntityUid partId,
        string slotId,
        BodyPartType partType,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            return null;
        var container = _container.EnsureContainer<ContainerSlot>(partId, GetSlotContainerName(slotId));
        part.Children.Add(slotId, new BodyPartSlot(slotId, partType, container));
        return container;
    }

    public bool TryCreatePartSlot(
        EntityUid? partId,
        string slotId,
        BodyPartType partType,
        [NotNullWhen(true)] out BodyPartSlot? slot,
        BodyPartComponent? part = null)
    {
        slot = null;

        if (partId == null ||
            !Resolve(partId.Value, ref part, false))
            return false;
        var container = _container.EnsureContainer<ContainerSlot>(partId.Value, GetSlotContainerName(slotId));
        slot = new BodyPartSlot(slotId, partType, container);
        return part.Children.TryAdd(slotId,slot.Value);
    }

    public bool TryCreatePartSlotAndAttach(
        EntityUid? parentId,
        string id,
        EntityUid? childId,
        BodyPartType partType,
        BodyPartComponent? parent = null,
        BodyPartComponent? child = null)
    {
        return TryCreatePartSlot(parentId, id, partType, out var slot, parent) && AttachPart(parentId, slot, childId, parent, child);
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetPartChildren(EntityUid? id, BodyPartComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            yield break;

        foreach (var (slotId, slot) in part.Children)
        {
            if (!TryComp(slot.Entity, out BodyPartComponent? childPart))
                continue;

            yield return (slot.Entity.Value, childPart);

            foreach (var subChild in GetPartChildren(slot.Entity, childPart))
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
            if (!TryComp(slot.Container.ContainedEntity, out OrganComponent? organ))
                continue;

            yield return (slot.Container.ContainedEntity.Value, organ);
        }
    }

    public IEnumerable<BodyPartSlot> GetPartAllSlots(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null ||
            !Resolve(partId.Value, ref part, false))
            yield break;

        foreach (var (slotId,slot) in part.Children)
        {
            yield return slot;

            if (!TryComp(slot.Entity, out BodyComponent? childPart))
                continue;

            foreach (var subChild in GetBodyAllSlots(slot.Entity, childPart))
            {
                yield return subChild;
            }
        }
    }

    public bool CanAttachPart(EntityUid? parentId, BodyPartSlot? slot, EntityUid? partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (partId == null || parentId == null || !Resolve(partId.Value, ref part, false) ||
            !Resolve(parentId.Value, ref parentPart, false) || slot == null)
        {
            return false;
        }
        return CanAttachPart(parentId.Value, slot.Value.Id, partId, parentPart, part);
    }

    public bool CanAttachPart(EntityUid? parentId, string slotId, EntityUid? partId, BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (partId == null || parentId == null || !Resolve(partId.Value, ref part, false) || !Resolve(parentId.Value, ref parentPart, false)
            || !parentPart.Children.TryGetValue(slotId, out var parentSlotData))
        {
            return false;
        }

        return (part.PartType == parentSlotData.Type) && Containers.TryGetContainer(parentId.Value, GetSlotContainerName(slotId), out var container) &&
               _container.CanInsert(partId.Value, container);
    }

    public bool DetachPart(EntityUid? partId, BodyPartComponent? part = null, BodyPartComponent? parentPart = null,
        bool reparent = false, EntityCoordinates? destination = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false) || part.Parent == null
            || !Resolve(part.Parent.Value, ref parentPart) || part.SlotId == null || !parentPart.Children.Remove(part.SlotId, out var slotData))
            return false;
        var ev = new BodyPartRemovedEvent(part.SlotId, part);
        RaiseLocalEvent(partId.Value, ref ev, true);
        var oldParent = part.Parent;
        var oldBody = part.Body;
        var oldSlotId = part.SlotId;
        part.SlotId = null;
        part.Parent = null;
        part.Body = null;
        Dirty(partId.Value, part);
        Dirty(oldParent.Value, parentPart);

        if (oldBody is { } body)
        {

            var args = new BodyPartRemovedEvent(oldSlotId, part);
            RaiseLocalEvent(body, ref args);

            if (part.PartType == BodyPartType.Leg)
            {
                UpdateMovementSpeed(body);
                if(!GetBodyChildrenOfType(oldBody, BodyPartType.Leg).Any())
                    Standing.Down(body);
            }

            if (part.IsVital && !GetBodyChildrenOfType(body, part.PartType).Any())
            {
                // TODO BODY SYSTEM KILL : remove this when wounding and required parts are implemented properly
                var damage = new DamageSpecifier(Prototypes.Index<DamageTypePrototype>("Bloodloss"), 300);
                Damageable.TryChangeDamage(body, damage);
            }

            foreach (var (organId, _) in GetPartOrgans(partId, part))
            {
                RaiseLocalEvent(organId, new RemovedFromBodyEvent(body), true);
            }
            DirtyAllComponents(body);
        }

        return slotData.Container.Remove(partId.Value, EntityManager, reparent: reparent, destination: destination);
    }

    public virtual bool AttachPart(EntityUid? parentPartId, BodyPartSlot? slot, EntityUid? partId,
        BodyPartComponent? part = null, BodyPartComponent? parentPart = null)
    {
        if (parentPartId == null || partId == null || slot == null || !Resolve(parentPartId.Value, ref parentPart, false) ||
            !Resolve(partId.Value, ref part, false) ||
            !CanAttachPart(parentPartId, slot.Value.Id, partId, parentPart, part))
            return false;

        if (part.Parent != null)
        {
            DetachPart(partId, part, parentPart);
        }
        parentPart.Children[slot.Value.Id].Container.Insert(partId.Value);
        part.Body = parentPart.Body;
        Dirty(partId.Value, part);
        var ev = new BodyPartAddedEvent(slot.Value.Id, part);
        RaiseLocalEvent(parentPartId.Value, ref ev, true);
        if (part.Body is { } body)
        {
            if (part.PartType == BodyPartType.Leg)
                UpdateMovementSpeed(body);

            var partAddedEvent = new BodyPartAddedEvent(slot.Value.Id, part);
            RaiseLocalEvent(body, ref partAddedEvent);

            // TODO: Body refactor. Somebody is doing it
            // EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartAdded(Owner, argsAdded);

            foreach (var (organId, _) in GetPartOrgans(parentPartId, part))
            {
                RaiseLocalEvent(organId, new AddedToBodyEvent(body), true);
            }
            DirtyAllComponents(body);
        }
        return true;
    }


    public virtual bool AttachPart( EntityUid? parentPartId, string slotId, EntityUid? partId, BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (parentPartId == null || partId == null || !Resolve(parentPartId.Value, ref parentPart, false)
            || parentPart.Children.TryGetValue(slotId, out var slot))
            return false;
        return AttachPart(parentPartId, slot, partId, parentPart, part);
    }

    public virtual bool DropPart(EntityUid? partId, BodyPartComponent? part = null, BodyPartComponent? parentPart = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false) || part.Parent == null ||
            !Resolve(part.Parent.Value, ref parentPart) || DetachPart(partId, part, parentPart))
            return false;
        SharedTransform.AttachToGridOrMap(partId.Value);
        partId.Value.RandomOffset(0.25f);
        return true;
    }

    public void UpdateMovementSpeed(EntityUid bodyId, BodyComponent? body = null, MovementSpeedModifierComponent? movement = null)
    {
        if (!Resolve(bodyId, ref body, ref movement, false))
            return;

        if (body.RequiredLegs <= 0)
            return;

        if (body.RootPart.ContainedEntity is not { } rootPart)
            return;

        var allSlots = GetAllBodyPartSlots(rootPart).ToHashSet();
        var allLegs = new HashSet<EntityUid>();
        foreach (var slot in allSlots)
        {
            if (slot is {Type: BodyPartType.Leg, Entity: {  } child})
                allLegs.Add(child);
        }

        var walkSpeed = 0f;
        var sprintSpeed = 0f;
        var acceleration = 0f;
        foreach (var leg in allLegs)
        {
            if (!TryComp<MovementBodyPartComponent>(leg, out var legModifier))
                continue;

            walkSpeed += legModifier.WalkSpeed;
            sprintSpeed += legModifier.SprintSpeed;
            acceleration += legModifier.Acceleration;
        }

        walkSpeed /= body.RequiredLegs;
        sprintSpeed /= body.RequiredLegs;
        acceleration /= body.RequiredLegs;
        Movement.ChangeBaseSpeed(bodyId, walkSpeed, sprintSpeed, acceleration, movement);
    }

    public bool DropPartAt(EntityUid? partId, EntityCoordinates dropAt, BodyPartComponent? part = null)
    {
        if (partId == null || !DropPart(partId, part))
            return false;

        if (TryComp(partId.Value, out TransformComponent? transform))
            SharedTransform.SetCoordinates(partId.Value,dropAt);
        return true;
    }

    public bool OrphanPart(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false))
            return false;

        DropPart(partId, part);

        foreach (var (_, slot) in part.Children)
        {
            DropPart(slot.Entity);
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


    public bool PartHasChild(EntityUid? parentId, EntityUid? childId, BodyPartComponent? parent,
        BodyPartComponent? child)
    {
        if (parentId == null || childId == null || !Resolve(parentId.Value, ref parent, false) ||
            !Resolve(childId.Value, ref child, false))
            return false;
        foreach (var (foundId, _) in GetPartChildren(parentId, parent))
        {
            if (foundId == childId)
                return true;
        }
        return false;
    }

    public bool BodyHasChild(
        EntityUid? bodyId,
        EntityUid? partId,
        BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false) || partId == null ||
            body.RootPart.ContainedEntity == null || !Resolve(partId.Value, ref part, false) ||
            !TryComp(body.RootPart.ContainedEntity.Value, out BodyPartComponent? rootPart)
            )
            return false;
        return PartHasChild(body.RootPart.ContainedEntity, partId, rootPart, part);
    }

    public IEnumerable<EntityUid> GetBodyPartAdjacentParts(EntityUid partId, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            yield break;

        if (part.Parent != null)
            yield return part.Parent.Value;

        foreach (var (slotId, slot) in part.Children)
        {
            if (slot.Entity != null)
                yield return slot.Entity.Value;
        }
    }

    public IEnumerable<(EntityUid AdjacentId, T Component)> GetBodyPartAdjacentPartsComponents<T>(
        EntityUid partId,
        BodyPartComponent? part = null)
        where T : Component
    {
        if (!Resolve(partId, ref part, false))
            yield break;

        var query = GetEntityQuery<T>();
        foreach (var adjacentId in GetBodyPartAdjacentParts(partId, part))
        {
            if (query.TryGetComponent(adjacentId, out var component))
                yield return (adjacentId, component);
        }
    }

    public bool TryGetBodyPartAdjacentPartsComponents<T>(
        EntityUid partId,
        [NotNullWhen(true)] out List<(EntityUid AdjacentId, T Component)>? comps,
        BodyPartComponent? part = null)
        where T : Component
    {
        if (!Resolve(partId, ref part, false))
        {
            comps = null;
            return false;
        }

        var query = GetEntityQuery<T>();
        comps = new List<(EntityUid AdjacentId, T Component)>();
        foreach (var adjacentId in GetBodyPartAdjacentParts(partId, part))
        {
            if (query.TryGetComponent(adjacentId, out var component))
                comps.Add((adjacentId, component));
        }

        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }

    /// <summary>
    ///     Returns a list of ValueTuples of <see cref="T"/> and OrganComponent on each organ
    ///     in the given part.
    /// </summary>
    /// <param name="uid">The part entity id to check on.</param>
    /// <param name="part">The part to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    public List<(T Comp, OrganComponent Organ)> GetBodyPartOrganComponents<T>(
        EntityUid uid,
        BodyPartComponent? part = null)
        where T : Component
    {
        if (!Resolve(uid, ref part))
            return new List<(T Comp, OrganComponent Organ)>();

        var query = GetEntityQuery<T>();
        var list = new List<(T Comp, OrganComponent Organ)>();
        foreach (var organ in GetPartOrgans(uid, part))
        {
            if (query.TryGetComponent(organ.Id, out var comp))
                list.Add((comp, organ.Component));
        }

        return list;
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and OrganComponent on each organs
    ///     in the given part.
    /// </summary>
    /// <param name="uid">The part entity id to check on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="part">The part to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetBodyPartOrganComponents<T>(
        EntityUid uid,
        [NotNullWhen(true)] out List<(T Comp, OrganComponent Organ)>? comps,
        BodyPartComponent? part = null)
        where T : Component
    {
        if (!Resolve(uid, ref part))
        {
            comps = null;
            return false;
        }

        comps = GetBodyPartOrganComponents<T>(uid, part);

        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }
}
