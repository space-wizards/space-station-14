using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Robust.Shared.Containers;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void AddOrgan(EntityUid uid, EntityUid bodyUid, EntityUid parentPartUid, OrganComponent component)
    {
        component.Body = bodyUid;
        RaiseLocalEvent(uid, new AddedToPartEvent(parentPartUid));

        if (component.Body != null)
            RaiseLocalEvent(uid, new AddedToPartInBodyEvent(component.Body.Value, parentPartUid));

        Dirty(uid, component);
    }

    private void RemoveOrgan(EntityUid uid, EntityUid parentPartUid, OrganComponent component)
    {
        RaiseLocalEvent(uid, new RemovedFromPartEvent(parentPartUid));

        if (component.Body != null)
        {
            RaiseLocalEvent(uid, new RemovedFromPartInBodyEvent(component.Body.Value, parentPartUid));
        }

        component.Body = null;
        Dirty(uid, component);
    }

    /// <summary>
    /// Creates the specified organ slot on the parent entity.
    /// </summary>
    private OrganSlot? CreateOrganSlot(string slotId, EntityUid parent, BodyPartComponent? part = null)
    {
        if (!Resolve(parent, ref part, false))
            return null;

        Containers.EnsureContainer<ContainerSlot>(parent, GetOrganContainerId(slotId));
        var slot = new OrganSlot(slotId);
        part.Organs.Add(slotId, slot);
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

        if (parent == null || !Resolve(parent.Value, ref part, false))
        {
            return false;
        }

        Containers.EnsureContainer<ContainerSlot>(parent.Value, GetOrganContainerId(slotId));
        slot = new OrganSlot(slotId);
        return part.Organs.TryAdd(slotId,slot.Value);
    }

    /// <summary>
    /// Returns whether the slotId exists on the partId.
    /// </summary>
    public bool CanInsertOrgan(EntityUid partId, string slotId, BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part) && part.Organs.ContainsKey(slotId);
    }

    /// <summary>
    /// Returns whether the specified organ slot exists on the partId.
    /// </summary>
    public bool CanInsertOrgan(EntityUid partId, OrganSlot slot, BodyPartComponent? part = null)
    {
        return CanInsertOrgan(partId, slot.Id, part);
    }

    public bool InsertOrgan(EntityUid partId, EntityUid organId, string slotId, BodyPartComponent? part = null, OrganComponent? organ = null)
    {
        if (!Resolve(organId, ref organ, false) ||
            !Resolve(partId, ref part, false) ||
            !CanInsertOrgan(partId, slotId, part))
        {
            return false;
        }

        var containerId = GetOrganContainerId(slotId);

        if (!Containers.TryGetContainer(partId, containerId, out var container))
            return false;

        return Containers.Insert(organId, container);
    }

    /// <summary>
    /// Removes the organ if it is inside of a body part.
    /// </summary>
    public bool RemoveOrgan(EntityUid organId, OrganComponent? organ = null)
    {
        if (!Containers.TryGetContainingContainer(organId, out var container))
            return false;

        var parent = container.Owner;

        if (!HasComp<BodyPartComponent>(parent))
            return false;

        return Containers.Remove(organId, container);
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
        if (!Resolve(partId, ref part, false) ||
            !Resolve(organId, ref organ, false))
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
    ///     Returns a list of ValueTuples of <see cref="T"/> and OrganComponent on each organ
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The body entity id to check on.</param>
    /// <param name="body">The body to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    public List<(T Comp, OrganComponent Organ)> GetBodyOrganComponents<T>(
        EntityUid uid,
        BodyComponent? body = null)
        where T : IComponent
    {
        if (!Resolve(uid, ref body))
            return new List<(T Comp, OrganComponent Organ)>();

        var query = GetEntityQuery<T>();
        var list = new List<(T Comp, OrganComponent Organ)>(3);
        foreach (var organ in GetBodyOrgans(uid, body))
        {
            if (query.TryGetComponent(organ.Id, out var comp))
                list.Add((comp, organ.Component));
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
    public bool TryGetBodyOrganComponents<T>(
        EntityUid uid,
        [NotNullWhen(true)] out List<(T Comp, OrganComponent Organ)>? comps,
        BodyComponent? body = null)
        where T : IComponent
    {
        if (!Resolve(uid, ref body))
        {
            comps = null;
            return false;
        }

        comps = GetBodyOrganComponents<T>(uid, body);

        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }
}
