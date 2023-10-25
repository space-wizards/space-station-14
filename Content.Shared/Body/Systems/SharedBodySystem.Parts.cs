using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeParts()
    {
        // TODO: This doesn't handle comp removal on child ents.

        // If you modify this also see the Body partial for root parts.
        SubscribeLocalEvent<BodyPartComponent, EntInsertedIntoContainerMessage>(OnBodyPartInserted);
        SubscribeLocalEvent<BodyPartComponent, EntRemovedFromContainerMessage>(OnBodyPartRemoved);
    }

    private void OnBodyPartInserted(EntityUid uid, BodyPartComponent component, EntInsertedIntoContainerMessage args)
    {
        // Body part inserted into another body part.
        var entity = args.Entity;
        var slotId = args.Container.ID;

        if (component.Body != null)
        {
            if (TryComp(entity, out BodyPartComponent? childPart))
            {
                AddPart(component.Body.Value, entity, slotId, childPart);
                RecursiveBodyUpdate(entity, component.Body.Value, childPart);
            }

            if (TryComp(entity, out OrganComponent? organ))
            {
                AddOrgan(entity, component.Body.Value, uid, organ);
            }
        }
    }

    private void OnBodyPartRemoved(EntityUid uid, BodyPartComponent component, EntRemovedFromContainerMessage args)
    {
        // TODO: lifestage shenanigans
        if (TerminatingOrDeleted(uid))
            return;

        // Body part removed from another body part.
        var entity = args.Entity;
        var slotId = args.Container.ID;

        if (TryComp(entity, out BodyPartComponent? childPart) && childPart.Body != null)
        {
            RemovePart(childPart.Body.Value, entity, slotId, childPart);
            RecursiveBodyUpdate(entity, null, childPart);
        }

        if (TryComp(entity, out OrganComponent? organ))
        {
            RemoveOrgan(entity, uid, organ);
        }
    }

    private void RecursiveBodyUpdate(EntityUid uid, EntityUid? bodyUid, BodyPartComponent component)
    {
        foreach (var children in GetBodyPartChildren(uid, component))
        {
            if (children.Component.Body != bodyUid)
            {
                children.Component.Body = bodyUid;
                Dirty(children.Id, children.Component);

                foreach (var slotId in children.Component.Organs.Keys)
                {
                    var organContainerId = GetOrganContainerId(slotId);

                    if (!Containers.TryGetContainer(children.Id, organContainerId, out var container))
                        continue;

                    foreach (var organ in container.ContainedEntities)
                    {
                        if (TryComp(organ, out OrganComponent? organComp))
                        {
                            var oldBody = organComp.Body;
                            organComp.Body = bodyUid;

                            if (bodyUid != null)
                            {
                                var ev = new AddedToPartInBodyEvent(bodyUid.Value, children.Id);
                                RaiseLocalEvent(organ, ev);
                            }
                            else if (oldBody != null)
                            {
                                var ev = new RemovedFromPartInBodyEvent(oldBody.Value, children.Id);
                                RaiseLocalEvent(organ, ev);
                            }

                            Dirty(organ, organComp);
                        }
                    }
                }
            }
        }
    }

    protected virtual void AddPart(
        EntityUid bodyUid,
        EntityUid partUid,
        string slotId,
        BodyPartComponent component,
        BodyComponent? bodyComp = null)
    {
        DebugTools.AssertOwner(partUid, component);
        Dirty(partUid, component);
        component.Body = bodyUid;

        var ev = new BodyPartAddedEvent(slotId, component);
        RaiseLocalEvent(bodyUid, ref ev);

        AddLeg(partUid, bodyUid, component, bodyComp);
    }

    protected virtual void RemovePart(
        EntityUid bodyUid,
        EntityUid partUid,
        string slotId,
        BodyPartComponent component,
        BodyComponent? bodyComp = null)
    {
        DebugTools.AssertOwner(partUid, component);
        Resolve(bodyUid, ref bodyComp, false);
        Dirty(partUid, component);
        component.Body = null;

        var ev = new BodyPartRemovedEvent(slotId, component);
        RaiseLocalEvent(bodyUid, ref ev);

        RemoveLeg(partUid, bodyUid, component);
        PartRemoveDamage(bodyUid, component, bodyComp);
    }

    private void AddLeg(EntityUid uid, EntityUid bodyUid, BodyPartComponent component, BodyComponent? bodyComp = null)
    {
        if (!Resolve(bodyUid, ref bodyComp, false))
            return;

        if (component.PartType == BodyPartType.Leg)
        {
            bodyComp.LegEntities.Add(uid);
            UpdateMovementSpeed(bodyUid);
            Dirty(bodyUid, bodyComp);
        }
    }

    private void RemoveLeg(EntityUid uid, EntityUid bodyUid, BodyPartComponent component, BodyComponent? bodyComp = null)
    {
        if (!Resolve(bodyUid, ref bodyComp, false))
            return;

        if (component.PartType == BodyPartType.Leg)
        {
            bodyComp.LegEntities.Remove(uid);
            UpdateMovementSpeed(bodyUid);
            Dirty(bodyUid, bodyComp);

            if (!bodyComp.LegEntities.Any())
            {
                Standing.Down(bodyUid);
            }
        }
    }

    private void PartRemoveDamage(EntityUid parent, BodyPartComponent component, BodyComponent? bodyComp = null)
    {
        if (!Resolve(parent, ref bodyComp, false))
            return;

        if (component.IsVital && !GetBodyChildrenOfType(parent, component.PartType, bodyComp).Any())
        {
            // TODO BODY SYSTEM KILL : remove this when wounding and required parts are implemented properly
            var damage = new DamageSpecifier(Prototypes.Index<DamageTypePrototype>("Bloodloss"), 300);
            Damageable.TryChangeDamage(parent, damage);
        }
    }

    /// <summary>
    /// Tries to get the parent body part to this if applicable.
    /// Doesn't validate if it's a part of body system.
    /// </summary>
    public EntityUid? GetParentPartOrNull(EntityUid uid)
    {
        if (!Containers.TryGetContainingContainer(uid, out var container))
            return null;

        var parent = container.Owner;

        if (!HasComp<BodyPartComponent>(parent))
            return null;

        return parent;
    }

    /// <summary>
    /// Tries to get the parent body part and slot to this if applicable.
    /// </summary>
    public (EntityUid Parent, string Slot)? GetParentPartAndSlotOrNull(EntityUid uid)
    {
        if (!Containers.TryGetContainingContainer(uid, out var container))
            return null;

        var slotId = GetPartSlotContainerIdFromContainer(container.ID);

        if (string.IsNullOrEmpty(slotId))
            return null;

        var parent = container.Owner;

        if (!TryComp<BodyPartComponent>(parent, out var parentBody) || !parentBody.Children.ContainsKey(slotId))
            return null;

        return (parent, slotId);
    }

    /// <summary>
    /// Tries to get the relevant parent body part to this if it exists.
    /// It won't exist if this is the root body part or if it's not in a body.
    /// </summary>
    public bool TryGetParentBodyPart(
        EntityUid partUid,
        [NotNullWhen(true)] out EntityUid? parentUid,
        [NotNullWhen(true)] out BodyPartComponent? parentComponent)
    {
        DebugTools.Assert(HasComp<BodyPartComponent>(partUid));
        parentUid = null;
        parentComponent = null;

        if (Containers.TryGetContainingContainer(partUid, out var container) &&
            TryComp(container.Owner, out parentComponent))
        {
            parentUid = container.Owner;
            return true;
        }

        return false;
    }

    #region Slots

    /// <summary>
    /// Creates a BodyPartSlot on the specified partUid.
    /// </summary>
    private BodyPartSlot? CreatePartSlot(
        EntityUid partUid,
        string slotId,
        BodyPartType partType,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partUid, ref part, false))
            return null;

        Containers.EnsureContainer<ContainerSlot>(partUid, GetPartSlotContainerId(slotId));
        var partSlot = new BodyPartSlot(slotId, partType);
        part.Children.Add(slotId, partSlot);
        Dirty(partUid, part);
        return partSlot;
    }

    /// <summary>
    /// Tries to create a BodyPartSlot on the specified partUid.
    /// </summary>
    /// <returns>false if not relevant or can't add it.</returns>
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
        {
            return false;
        }

        Containers.EnsureContainer<ContainerSlot>(partId.Value, GetPartSlotContainerId(slotId));
        slot = new BodyPartSlot(slotId, partType);

        if (!part.Children.TryAdd(slotId, slot.Value))
            return false;

        Dirty(partId.Value, part);
        return true;
    }

    public bool TryCreatePartSlotAndAttach(
        EntityUid parentId,
        string slotId,
        EntityUid childId,
        BodyPartType partType,
        BodyPartComponent? parent = null,
        BodyPartComponent? child = null)
    {
        return TryCreatePartSlot(parentId, slotId, partType, out _, parent)
               && AttachPart(parentId, slotId, childId, parent, child);
    }

    #endregion

    #region RootPartManagement

    /// <summary>
    /// Returns true if the partId is the root body container for the specified bodyId.
    /// </summary>
    public bool IsPartRoot(EntityUid bodyId, EntityUid partId, BodyComponent? body = null, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part)|| !Resolve(bodyId, ref body))
            return false;

        return Containers.TryGetContainingContainer(bodyId, partId, out var container) && container.ID == BodyRootContainerId;
    }

    /// <summary>
    /// Returns true if we can attach the partId to the bodyId as the root entity.
    /// </summary>
    public bool CanAttachToRoot(EntityUid bodyId, EntityUid partId, BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        return Resolve(bodyId, ref body) &&
               Resolve(partId, ref part) &&
               body.RootContainer.ContainedEntity == null &&
               bodyId != part.Body;
    }

    /// <summary>
    /// Returns the root part of this body if it exists.
    /// </summary>
    public (EntityUid Entity, BodyPartComponent BodyPart)? GetRootPartOrNull(EntityUid bodyId, BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body) || body.RootContainer.ContainedEntity == null)
            return null;

        return (body.RootContainer.ContainedEntity.Value,
            Comp<BodyPartComponent>(body.RootContainer.ContainedEntity.Value));
    }

    /// <summary>
    /// Returns true if the partId can be attached to the parentId in the specified slot.
    /// </summary>
    public bool CanAttachPart(
        EntityUid parentId,
        BodyPartSlot slot,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false) ||
            !Resolve(parentId, ref parentPart, false))
        {
            return false;
        }

        return CanAttachPart(parentId, slot.Id, partId, parentPart, part);
    }

    /// <summary>
    /// Returns true if we can attach the specified partId to the parentId in the specified slot.
    /// </summary>
    public bool CanAttachPart(
        EntityUid parentId,
        string slotId,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false) ||
            !Resolve(parentId, ref parentPart, false) ||
            !parentPart.Children.TryGetValue(slotId, out var parentSlotData))
        {
            return false;
        }

        return part.PartType == parentSlotData.Type &&
               Containers.TryGetContainer(parentId, GetPartSlotContainerId(slotId), out var container) &&
               Containers.CanInsert(partId, container);
    }

    public bool AttachPartToRoot(
        EntityUid bodyId,
        EntityUid partId,
        BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(bodyId, ref body) ||
            !Resolve(partId, ref part) ||
            !CanAttachToRoot(bodyId, partId, body, part))
        {
            return false;
        }

        return body.RootContainer.Insert(partId);
    }

    #endregion

    #region Attach/Detach

    /// <summary>
    /// Attaches a body part to the specified body part parent.
    /// </summary>
     public bool AttachPart(
         EntityUid parentPartId,
         string slotId,
         EntityUid partId,
         BodyPartComponent? parentPart = null,
         BodyPartComponent? part = null)
    {
        if (!Resolve(parentPartId, ref parentPart, false) ||
            !parentPart.Children.TryGetValue(slotId, out var slot))
        {
            return false;
        }

        return AttachPart(parentPartId, slot, partId, parentPart, part);
    }

    /// <summary>
    /// Attaches a body part to the specified body part parent.
    /// </summary>
    public bool AttachPart(
        EntityUid parentPartId,
        BodyPartSlot slot,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(parentPartId, ref parentPart, false) ||
            !Resolve(partId, ref part, false) ||
            !CanAttachPart(parentPartId, slot.Id, partId, parentPart, part) ||
            !parentPart.Children.ContainsKey(slot.Id))
        {
            return false;
        }

        if (!Containers.TryGetContainer(parentPartId, GetPartSlotContainerId(slot.Id), out var container))
        {
            DebugTools.Assert($"Unable to find body slot {slot.Id} for {ToPrettyString(parentPartId)}");
            return false;
        }

        return container.Insert(partId);
    }

    #endregion

    #region Misc

    public void UpdateMovementSpeed(EntityUid bodyId, BodyComponent? body = null, MovementSpeedModifierComponent? movement = null)
    {
        if (!Resolve(bodyId, ref body, ref movement, false))
            return;

        if (body.RequiredLegs <= 0)
            return;

        var walkSpeed = 0f;
        var sprintSpeed = 0f;
        var acceleration = 0f;
        foreach (var legEntity in body.LegEntities)
        {
            if (!TryComp<MovementBodyPartComponent>(legEntity, out var legModifier))
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

    /// <summary>
    /// Get all organs for the specified body part.
    /// </summary>
    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetPartOrgans(EntityUid partId, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            yield break;

        foreach (var slotId in part.Organs.Keys)
        {
            var containerSlotId = GetOrganContainerId(slotId);

            if (!Containers.TryGetContainer(partId, containerSlotId, out var container))
                continue;

            foreach (var containedEnt in container.ContainedEntities)
            {
                if (!TryComp(containedEnt, out OrganComponent? organ))
                    continue;

                yield return (containedEnt, organ);
            }
        }
    }

    /// <summary>
    /// Gets all BaseContainers for body parts on this entity and its child entities.
    /// </summary>
    public IEnumerable<BaseContainer> GetPartContainers(EntityUid id, BodyPartComponent? part = null)
    {
        if (!Resolve(id, ref part, false) ||
            part.Children.Count == 0)
        {
            yield break;
        }

        foreach (var slotId in part.Children.Keys)
        {
            var containerSlotId = GetPartSlotContainerId(slotId);

            if (!Containers.TryGetContainer(id, containerSlotId, out var container))
                continue;

            yield return container;

            foreach (var ent in container.ContainedEntities)
            {
                foreach (var childContainer in GetPartContainers(ent))
                {
                    yield return childContainer;
                }
            }
        }
    }

    /// <summary>
    /// Returns all body part components for this entity including itself.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyPartChildren(EntityUid partId, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            yield break;

        yield return (partId, part);

        foreach (var slotId in part.Children.Keys)
        {
            var containerSlotId = GetPartSlotContainerId(slotId);

            if (Containers.TryGetContainer(partId, containerSlotId, out var container))
            {
                foreach (var containedEnt in container.ContainedEntities)
                {
                    if (!TryComp(containedEnt, out BodyPartComponent? childPart))
                        continue;

                    foreach (var value in GetBodyPartChildren(containedEnt, childPart))
                    {
                        yield return value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns all body part slots for this entity.
    /// </summary>
    public IEnumerable<BodyPartSlot> GetAllBodyPartSlots(EntityUid partId, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            yield break;

        foreach (var (slotId, slot) in part.Children)
        {
            yield return slot;

            var containerSlotId = GetOrganContainerId(slotId);

            if (Containers.TryGetContainer(partId, containerSlotId, out var container))
            {
                foreach (var containedEnt in container.ContainedEntities)
                {
                    if (!TryComp(containedEnt, out BodyPartComponent? childPart))
                        continue;

                    foreach (var subSlot in GetAllBodyPartSlots(containedEnt, childPart))
                    {
                        yield return subSlot;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns true if the bodyId has any parts of this type.
    /// </summary>
    public bool BodyHasPartType(EntityUid bodyId, BodyPartType type, BodyComponent? body = null)
    {
        return GetBodyChildrenOfType(bodyId, type, body).Any();
    }

    /// <summary>
    /// Returns true if the parentId has the specified childId.
    /// </summary>
    public bool PartHasChild(
        EntityUid parentId,
        EntityUid childId,
        BodyPartComponent? parent,
        BodyPartComponent? child)
    {
        if (!Resolve(parentId, ref parent, false) ||
            !Resolve(childId, ref child, false))
        {
            return false;
        }

        foreach (var (foundId, _) in GetBodyPartChildren(parentId, parent))
        {
            if (foundId == childId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the bodyId has the specified partId.
    /// </summary>
    public bool BodyHasChild(
        EntityUid bodyId,
        EntityUid partId,
        BodyComponent? body = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(bodyId, ref body, false) ||
            body.RootContainer.ContainedEntity == null ||
            !Resolve(partId, ref part, false) ||
            !TryComp(body.RootContainer.ContainedEntity, out BodyPartComponent? rootPart))
        {
            return false;
        }

        return PartHasChild(body.RootContainer.ContainedEntity.Value, partId, rootPart, part);
    }

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildrenOfType(
        EntityUid bodyId,
        BodyPartType type,
        BodyComponent? body = null)
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
        where T : IComponent
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
        where T : IComponent
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

    /// <summary>
    /// Gets the parent body part and all immediate child body parts for the partId.
    /// </summary>
    public IEnumerable<EntityUid> GetBodyPartAdjacentParts(EntityUid partId, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            yield break;

        if (TryGetParentBodyPart(partId, out var parentUid, out _))
            yield return parentUid.Value;

        foreach (var slotId in part.Children.Keys)
        {
            var container = Containers.GetContainer(partId, GetPartSlotContainerId(slotId));

            foreach (var containedEnt in container.ContainedEntities)
            {
                yield return containedEnt;
            }
        }
    }

    public IEnumerable<(EntityUid AdjacentId, T Component)> GetBodyPartAdjacentPartsComponents<T>(
        EntityUid partId,
        BodyPartComponent? part = null)
        where T : IComponent
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
        where T : IComponent
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
