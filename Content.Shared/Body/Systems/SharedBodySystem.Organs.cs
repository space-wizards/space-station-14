using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeOrgans()
    {
    }

    private OrganSlot? CreateOrganSlot(string slotName, EntityUid parent, BodyPartComponent? part = null)
    {
        if (!Resolve(parent, ref part, false))
            return null;

        var container = Containers.EnsureContainer<ContainerSlot>(parent,GetOrganContainerName(slotName));
        var slot = new OrganSlot(slotName, container);
        part.Organs.Add(slotName, slot);
        return slot;
    }

    public bool TryCreateOrganSlot(
        EntityUid? partId,
        string slotId,
        [NotNullWhen(true)] out OrganSlot? slot,
        BodyPartComponent? part = null)
    {
        slot = null;
        if (partId == null ||
            !Resolve(partId.Value, ref part, false))
            return false;
        var container = Containers.EnsureContainer<ContainerSlot>(partId.Value, GetOrganContainerName(slotId));
        slot = new OrganSlot(slotId, container);
        return part.Organs.TryAdd(slotId,slot.Value);
    }

    public bool CanInsertOrgan(EntityUid? partId, string slotName, BodyPartComponent? part = null)
    {
        return partId != null && Resolve(partId.Value, ref part) && part.Organs.ContainsKey(slotName);
    }

    public bool CanInsertOrgan(EntityUid? partId, OrganSlot slot, BodyPartComponent? part = null)
    {
        return CanInsertOrgan(partId, slot.Name, part);
    }
    public bool InsertOrgan(EntityUid? partId, EntityUid? organId, string slotName, BodyPartComponent? part = null, OrganComponent? organ = null)
    {
        if (organId == null || partId == null || !Resolve(organId.Value, ref organ, false) || organ.AttachedToSlot != null
            || !Resolve(partId.Value, ref part, false) || !CanInsertOrgan(partId, slotName, part))
            return false;

        if (organ.Parent != null)
        {
            DropOrgan(organId, organ);
        }
        if (!part.Organs[slotName].Container.Insert(organId.Value))
            return false;
        organ.Parent = partId;
        organ.Body = part.Body;
        organ.AttachedToSlot = GetOrganContainerName(slotName);
        Dirty(partId.Value, part);
        Dirty(organId.Value, organ);

        RaiseLocalEvent(organId.Value, new AddedToPartEvent(partId.Value));
        if (organ.Body != null)
            RaiseLocalEvent(organId.Value, new AddedToPartInBodyEvent(organ.Body.Value, partId.Value));
        return true;
    }

    public bool ExtractOrgan(EntityUid? organId, OrganComponent? organ = null, BodyPartComponent? parentPart = null,
        bool reparent = false, EntityCoordinates? destination = null)
    {
        if (organId == null ||
            !Resolve(organId.Value, ref organ, false) || organ.AttachedToSlot == null ||
            organ.Parent is not { } parentPartId || !Resolve(parentPartId, ref parentPart) ||
            !parentPart.Organs.TryGetValue(organ.AttachedToSlot, out var organSlot))
            return false;
        var oldParent = organ.Parent;

        RaiseLocalEvent(organId.Value, new RemovedFromPartEvent(parentPartId),true);
        if (organ.Body is { } body)
            RaiseLocalEvent(organId.Value, new RemovedFromPartInBodyEvent(body, parentPartId), true);
        organ.Body = null;
        organ.Parent = null;
        return organSlot.Container.Remove(oldParent.Value, EntityManager, reparent: reparent, destination: destination);
    }

    public void DirtyAllComponents(EntityUid uid)
    {
        // TODO just use containers. Please
        if (TryComp(uid, out BodyPartComponent? part))
            Dirty(uid, part);

        if (TryComp(uid, out OrganComponent? organ))
            Dirty(uid, organ);

        if (TryComp(uid, out BodyComponent? body))
            Dirty(uid, body);
    }


    public bool AddOrganToFirstValidSlot(
        EntityUid? partId,
        EntityUid? organId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null
        )
    {
        if (partId == null || organId == null ||
            !Resolve(partId.Value, ref part, false) ||
            !Resolve(organId.Value, ref organ, false))
            return false;

        foreach (var slot in part.Organs.Values)
        {
            if (slot.Organ == null)
                continue;

            InsertOrgan(partId, organId, slot.Name, part, organ);
            return true;
        }

        return false;
    }



    public bool DropOrgan(EntityUid? organId, OrganComponent? organ = null, BodyPartComponent? parentPart = null)
    {
        if (organId == null ||
            !Resolve(organId.Value, ref organ, false) ||
            organ.Parent is not { } parentPartId || !Resolve(parentPartId, ref parentPart) ||
            !ExtractOrgan(organId, organ, parentPart))
            return false;
        SharedTransform.AttachToGridOrMap(organId.Value);
        organId.Value.RandomOffset(0.25f);
        return true;
    }

    public bool DropOrganAt(EntityUid? organId, EntityCoordinates dropAt, OrganComponent? organ = null)
    {
        if (organId == null || !DropOrgan(organId, organ))
            return false;

        if (TryComp(organId.Value, out TransformComponent? transform))
            SharedTransform.SetCoordinates(organId.Value, transform, dropAt);
        return true;
    }

    public bool DeleteOrgan(EntityUid? id, OrganComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            return false;

        DropOrgan(id, part);

        if (Deleted(id.Value))
            return false;

        Del(id.Value);
        return true;
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
        where T : Component
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
        where T : Component
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
