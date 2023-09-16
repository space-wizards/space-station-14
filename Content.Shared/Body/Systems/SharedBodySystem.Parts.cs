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
        SubscribeLocalEvent<BodyPartComponent, AfterAutoHandleStateEvent>(OnPartStateHandled);
    }
    private void OnPartStateHandled(EntityUid uid, BodyPartComponent component, ref AfterAutoHandleStateEvent args)
    {
        foreach (var (slotId, slotData) in component.Children)
        {
            component.Children[slotId] = slotData with
            {
                Container = Containers.EnsureContainer<ContainerSlot>(uid, GetBodySlotContainerName(slotData.Id))
            };
        }

        foreach (var (slotId, slotData) in component.Organs)
        {
            component.Organs[slotId] = slotData with
            {
                Container = Containers.EnsureContainer<ContainerSlot>(uid, GetBodySlotContainerName(slotData.Id))
            };
        }
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

    #region Slots

    private BodyPartSlot? CreatePartSlot(
        EntityUid partId,
        string slotId,
        BodyPartType partType,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            return null;
        var container = Containers.EnsureContainer<ContainerSlot>(partId, GetBodySlotContainerName(slotId));
        var partSlot = new BodyPartSlot(slotId, partType, container);
        part.Children.Add(slotId,partSlot );
        Dirty(partId, part);
        return partSlot;
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
        var container = Containers.EnsureContainer<ContainerSlot>(partId.Value, GetBodySlotContainerName(slotId));
        slot = new BodyPartSlot(slotId, partType, container);
        if (!part.Children.TryAdd(slotId, slot.Value))
            return false;
        Dirty(partId.Value, part);
        return true;
    }

    public bool TryCreatePartSlotAndAttach(
        EntityUid? parentId,
        string slotId,
        EntityUid? childId,
        BodyPartType partType,
        BodyPartComponent? parent = null,
        BodyPartComponent? child = null)
    {
        return TryCreatePartSlot(parentId, slotId, partType, out var slot, parent)
               && AttachPart(parentId, slot, childId, parent, child);
    }

    #endregion

    #region RootPartManagement

    public bool IsPartRoot(EntityUid? bodyId, EntityUid? partId, BodyComponent? body = null, BodyPartComponent? part = null)
    {
        if (bodyId == null || partId == null || !Resolve(partId.Value, ref part)|| !Resolve(bodyId.Value, ref body))
            return false;
        return part.Parent == null && body.RootContainer.ContainedEntity == partId;
    }

    public bool CanAttachToRoot(EntityUid? bodyId, EntityUid? partId, BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        return partId != null && bodyId != null && Resolve(bodyId.Value, ref body)
               && Resolve(partId.Value, ref part) && bodyId != part.Body;
    }

    public (EntityUid, BodyPartComponent)? GetRootPart(EntityUid? bodyId, BodyComponent? body = null)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body) || body.RootContainer.ContainedEntity == null)
            return null;
        return (body.RootContainer.ContainedEntity.Value,
            Comp<BodyPartComponent>(body.RootContainer.ContainedEntity.Value));
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

        return (part.PartType == parentSlotData.Type) && Containers.TryGetContainer(parentId.Value, GetBodySlotContainerName(slotId), out var container) &&
               Containers.CanInsert(partId.Value, container);
    }

    public bool AttachPartToRoot(EntityUid bodyId, EntityUid partId, string slotId, BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(bodyId, ref body) || !Resolve(partId, ref part) || !CanAttachToRoot(bodyId, partId, body, part))
            return false;
        body.RootContainer = Containers.EnsureContainer<ContainerSlot>(bodyId, BodyRootContainerId);
        body.RootPartSlot = slotId;
        if (part.Parent != null)
        {
            DetachPart(partId, part);
        }

        if (part.Body != bodyId && part.Body != null)
        {
            DetachPartFromRoot(part.Body, partId, null, part);
        }
        return InternalAttachPart(part.Body, null, partId, part, slotId, body.RootContainer);
    }
    public bool DetachPartFromRoot(EntityUid? bodyId, EntityUid? partId, BodyComponent? body = null,
        BodyPartComponent? part = null, bool reparent = false, EntityCoordinates? destination = null)
    {
        if (bodyId == null || partId == null || !Resolve(bodyId.Value, ref body) || !Resolve(partId.Value, ref part)
            || part.Parent != null || part.Body == null || part.Body != bodyId || part.SlotId == null)
            return false;
        return InternalDetachPart(bodyId, partId.Value, part, part.SlotId, body.RootContainer, reparent, destination);
    }


    #endregion

    #region Attach/Detach

     public virtual bool AttachPart( EntityUid? parentPartId, string slotId, EntityUid? partId, BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (parentPartId == null || partId == null || !Resolve(parentPartId.Value, ref parentPart, false)
            || parentPart.Children.TryGetValue(slotId, out var slot))
            return false;
        return AttachPart(parentPartId, slot, partId, parentPart, part);
    }

    public virtual bool AttachPart(EntityUid? parentPartId, BodyPartSlot? slot, EntityUid? partId,
        BodyPartComponent? parentPart = null, BodyPartComponent? part = null)
    {
        if (parentPartId == null || partId == null || slot == null || !Resolve(parentPartId.Value, ref parentPart, false) ||
            !Resolve(partId.Value, ref part, false) ||
            !CanAttachPart(parentPartId, slot.Value.Id, partId, parentPart, part)
            || !parentPart.Children.TryGetValue(slot.Value.Id, out var slotData))
            return false;
        if (part.Parent != null)
        {
            DetachPart(partId, part, parentPart);
        }
        return InternalAttachPart(part.Body, parentPartId, partId.Value, part, slotData.Id, slotData.Container);
    }

    protected virtual bool InternalAttachPart(EntityUid? bodyId, EntityUid? parentId, EntityUid partId ,BodyPartComponent part,
        string slotId, ContainerSlot container)
    {
        part.Body = bodyId;
        part.SlotId = slotId;
        Dirty(partId, part);
        var ev = new BodyPartAddedEvent(slotId, part);
        if (parentId != null)
            RaiseLocalEvent(parentId.Value, ref ev, true);
        if (part.Body is { } body)
        {
            if (part.PartType == BodyPartType.Leg)
                UpdateMovementSpeed(body);

            var partAddedEvent = new BodyPartAddedEvent(slotId, part);
            RaiseLocalEvent(body, ref partAddedEvent);

            // TODO: Body refactor. Somebody is doing it
            // EntitySystem.Get<SharedHumanoidAppearanceSystem>().BodyPartAdded(Owner, argsAdded);

            foreach (var (organId, _) in GetPartOrgans(partId, part))
            {
                RaiseLocalEvent(organId, new AddedToBodyEvent(body), true);
            }
            DirtyAllComponents(body);
        }
        return container.Insert(partId);
    }

    public bool DetachPart(EntityUid? partId, BodyPartComponent? part = null, BodyPartComponent? parentPart = null,
        bool reparent = false, EntityCoordinates? destination = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false) || part.Parent == null
            || !Resolve(part.Parent.Value, ref parentPart) || part.SlotId == null || !parentPart.Children.TryGetValue(part.SlotId, out var slotData))
            return false;
        return InternalDetachPart(part.Body, partId.Value, part, slotData.Id, slotData.Container, reparent, destination);
    }

    protected virtual bool InternalDetachPart(EntityUid? body, EntityUid partId, BodyPartComponent part, string slotId,
        ContainerSlot container, bool reparent,
        EntityCoordinates? coords)
    {
        var ev = new BodyPartRemovedEvent(slotId, part);
        RaiseLocalEvent(partId, ref ev, true);

        if (body != null)
        {
            var args = new BodyPartRemovedEvent(slotId, part);
            RaiseLocalEvent(body.Value, ref args);
            var bodyComp = Comp<BodyComponent>(body.Value);
            bodyComp.RootPartSlot = null;
            if (part.PartType == BodyPartType.Leg)
            {
                UpdateMovementSpeed(body.Value, bodyComp);
                if(!GetBodyChildrenOfType(body, BodyPartType.Leg, bodyComp).Any())
                    Standing.Down(body.Value);
            }

            if (part.IsVital && !GetBodyChildrenOfType(body, part.PartType, bodyComp).Any())
            {
                // TODO BODY SYSTEM KILL : remove this when wounding and required parts are implemented properly
                var damage = new DamageSpecifier(Prototypes.Index<DamageTypePrototype>("Bloodloss"), 300);
                Damageable.TryChangeDamage(body, damage);
            }

            foreach (var (organId, _) in GetPartOrgans(partId, part))
            {
                RaiseLocalEvent(organId, new RemovedFromBodyEvent(body.Value), true);
            }
            DirtyAllComponents(body.Value);
        }
        part.SlotId = null;
        part.Parent = null;
        part.Body = null;
        Dirty(partId, part);
        return container.Remove(partId, EntityManager, reparent: reparent, destination: coords);
    }

    #endregion

    #region Drop/Orphan/Delete

    public virtual bool DropPart(EntityUid? partId, BodyPartComponent? part = null, BodyPartComponent? parentPart = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part, false) || part.Parent == null ||
            !Resolve(part.Parent.Value, ref parentPart) || DetachPart(partId, part, parentPart))
            return false;
        SharedTransform.AttachToGridOrMap(partId.Value);
        partId.Value.RandomOffset(0.25f);
        return true;
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

    #endregion

    #region Misc

    public void UpdateMovementSpeed(EntityUid bodyId, BodyComponent? body = null, MovementSpeedModifierComponent? movement = null)
    {
        if (!Resolve(bodyId, ref body, ref movement, false))
            return;

        if (body.RequiredLegs <= 0)
            return;

        if (body.RootContainer.ContainedEntity is not { } rootPart)
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

    #endregion

    #region Queries

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
            body.RootContainer.ContainedEntity == null || !Resolve(partId.Value, ref part, false) ||
            !TryComp(body.RootContainer.ContainedEntity.Value, out BodyPartComponent? rootPart)
           )
            return false;
        return PartHasChild(body.RootContainer.ContainedEntity, partId, rootPart, part);
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildrenOfType(EntityUid? bodyId, BodyPartType type, BodyComponent? body = null)
    {
        foreach (var part in GetBodyChildren(bodyId, body))
        {
            if (part.Component.PartType == type)
                yield return part;
        }
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
    #endregion
}
