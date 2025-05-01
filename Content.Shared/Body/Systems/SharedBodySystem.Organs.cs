using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Robust.Shared.Containers;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void AddOrgan(
        Entity<OrganComponent> organEnt,
        EntityUid bodyUid,
        EntityUid parentPartUid)
    {
        organEnt.Comp.Body = bodyUid;
        var addedEv = new OrganAddedEvent(parentPartUid);
        RaiseLocalEvent(organEnt, ref addedEv);

        if (organEnt.Comp.Body is not null)
        {
            var addedInBodyEv = new OrganAddedToBodyEvent(bodyUid, parentPartUid);
            RaiseLocalEvent(organEnt, ref addedInBodyEv);
        }

        Dirty(organEnt, organEnt.Comp);
    }

    private void RemoveOrgan(Entity<OrganComponent> organEnt, EntityUid parentPartUid)
    {
        var removedEv = new OrganRemovedEvent(parentPartUid);
        RaiseLocalEvent(organEnt, ref removedEv);

        if (organEnt.Comp.Body is { Valid: true } bodyUid)
        {
            var removedInBodyEv = new OrganRemovedFromBodyEvent(bodyUid, parentPartUid);
            RaiseLocalEvent(organEnt, ref removedInBodyEv);
        }

        organEnt.Comp.Body = null;
        Dirty(organEnt, organEnt.Comp);
    }

    /// <summary>
    /// Creates the specified organ slot on the parent entity.
    /// </summary>
    private OrganSlot? CreateOrganSlot(Entity<BodyPartComponent?> parentEnt, string slotId)
    {
        if (!Resolve(parentEnt, ref parentEnt.Comp, logMissing: false))
            return null;

        Containers.EnsureContainer<ContainerSlot>(parentEnt, GetOrganContainerId(slotId));
        var slot = new OrganSlot(slotId);
        parentEnt.Comp.Organs.Add(slotId, slot);
        return slot;
    }

    /// <summary>
    /// Attempts to create the specified organ slot on the specified parent if it exists.
    /// </summary>
    public bool TryCreateOrganSlot(
        EntityUid? parent,
        string slotId,
        [NotNullWhen(true)] out OrganSlot? slot,
        BodyPartComponent? part = null)
    {
        slot = null;

        if (parent is null || !Resolve(parent.Value, ref part, logMissing: false))
        {
            return false;
        }

        Containers.EnsureContainer<ContainerSlot>(parent.Value, GetOrganContainerId(slotId));
        slot = new OrganSlot(slotId);
        return part.Organs.TryAdd(slotId, slot.Value);
    }

    /// <summary>
    /// Returns whether the slotId exists on the partId.
    /// </summary>
    public bool CanInsertOrgan(
        EntityUid partId,
        string slotId,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part) && part.Organs.ContainsKey(slotId);
    }

    /// <summary>
    /// Returns whether the specified organ slot exists on the partId.
    /// </summary>
    public bool CanInsertOrgan(
        EntityUid partId,
        OrganSlot slot,
        BodyPartComponent? part = null)
    {
        return CanInsertOrgan(partId, slot.Id, part);
    }

    public bool InsertOrgan(
        EntityUid partId,
        EntityUid organId,
        string slotId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null)
    {
        if (!Resolve(organId, ref organ, logMissing: false)
            || !Resolve(partId, ref part, logMissing: false)
            || !CanInsertOrgan(partId, slotId, part))
        {
            return false;
        }

        var containerId = GetOrganContainerId(slotId);

        return Containers.TryGetContainer(partId, containerId, out var container)
            && Containers.Insert(organId, container);
    }

    /// <summary>
    /// Removes the organ if it is inside of a body part.
    /// </summary>
    public bool RemoveOrgan(EntityUid organId, OrganComponent? organ = null)
    {
        if (!Containers.TryGetContainingContainer((organId, null, null), out var container))
            return false;

        var parent = container.Owner;

        return HasComp<BodyPartComponent>(parent)
            && Containers.Remove(organId, container);
    }

    /// <summary>
    /// Tries to add this organ to any matching slot on this body part.
    /// </summary>
    public bool AddOrganToFirstValidSlot(
        EntityUid partId,
        EntityUid organId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null)
    {
        if (!Resolve(partId, ref part, logMissing: false)
            || !Resolve(organId, ref organ, logMissing: false))
        {
            return false;
        }

        foreach (var slotId in part.Organs.Keys)
        {
            InsertOrgan(partId, organId, slotId, part, organ);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a list of Entity<<see cref="T"/>, <see cref="OrganComponent"/>>
    /// for each organ of the body
    /// </summary>
    /// <typeparam name="T">The component that we want to return</typeparam>
    /// <param name="entity">The body to check the organs of</param>
    public List<Entity<T, OrganComponent>> GetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity)
        where T : IComponent
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<Entity<T, OrganComponent>>();

        var query = GetEntityQuery<T>();
        var list = new List<Entity<T, OrganComponent>>(3);
        foreach (var organ in GetBodyOrgans(entity.Owner, entity.Comp))
        {
            if (query.TryGetComponent(organ.Id, out var comp))
                list.Add((organ.Id, comp, organ.Component));
        }

        return list;
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and OrganComponent on each organs
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The body entity id to check on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="body">The body to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity,
        [NotNullWhen(true)] out List<Entity<T, OrganComponent>>? comps)
        where T : IComponent
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
        {
            comps = null;
            return false;
        }

        comps = GetBodyOrganEntityComps<T>(entity);

        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }
}
