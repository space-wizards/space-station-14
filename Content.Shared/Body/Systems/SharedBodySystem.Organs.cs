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
        SubscribeLocalEvent<OrganComponent, ComponentGetState>(OnOrganGetState);
        SubscribeLocalEvent<OrganComponent, ComponentHandleState>(OnOrganHandleState);
    }

    private OrganSlot? CreateOrganSlot(string slotId, EntityUid parent, BodyPartComponent? part = null)
    {
        if (!Resolve(parent, ref part, false))
            return null;

        var slot = new OrganSlot(slotId, parent);
        part.Organs.Add(slotId, slot);

        return slot;
    }

    private bool CanInsertOrgan(EntityUid? organId, OrganSlot slot, OrganComponent? organ = null)
    {
        return organId != null &&
               slot.Child == null &&
               Resolve(organId.Value, ref organ, false) &&
               Containers.TryGetContainer(slot.Parent, BodyContainerId, out var container) &&
               container.CanInsert(organId.Value);
    }

    private void OnOrganGetState(EntityUid uid, OrganComponent organ, ref ComponentGetState args)
    {
        args.State = new OrganComponentState(organ.Body, organ.ParentSlot);
    }

    private void OnOrganHandleState(EntityUid uid, OrganComponent organ, ref ComponentHandleState args)
    {
        if (args.Current is not OrganComponentState state)
            return;

        organ.Body = state.Body;
        organ.ParentSlot = state.Parent;
    }

    public bool InsertOrgan(EntityUid? organId, OrganSlot slot, OrganComponent? organ = null)
    {
        if (organId == null ||
            !Resolve(organId.Value, ref organ, false) ||
            !CanInsertOrgan(organId, slot, organ))
            return false;

        DropOrgan(slot.Child);
        DropOrgan(organId, organ);

        var container = Containers.EnsureContainer<Container>(slot.Parent, BodyContainerId);
        if (!container.Insert(organId.Value))
            return false;

        slot.Child = organId;
        organ.ParentSlot = slot;
        organ.Body = CompOrNull<BodyPartComponent>(slot.Parent)?.Body;

        Dirty(slot.Parent);
        Dirty(organId.Value);

        if (organ.Body == null)
        {
            RaiseLocalEvent(organId.Value, new AddedToPartEvent(slot.Parent));
        }
        else
        {
            RaiseLocalEvent(organId.Value, new AddedToPartInBodyEvent(organ.Body.Value, slot.Parent));
        }

        return true;
    }

    public bool AddOrganToFirstValidSlot(
        EntityUid? childId,
        EntityUid? parentId,
        OrganComponent? child = null,
        BodyPartComponent? parent = null)
    {
        if (childId == null ||
            !Resolve(childId.Value, ref child, false) ||
            parentId == null ||
            !Resolve(parentId.Value, ref parent, false))
            return false;

        foreach (var slot in parent.Organs.Values)
        {
            if (slot.Child == null)
                continue;

            InsertOrgan(childId, slot, child);
            return true;
        }

        return false;
    }

    public bool DropOrgan(EntityUid? organId, OrganComponent? organ = null)
    {
        if (organId == null ||
            !Resolve(organId.Value, ref organ, false) ||
            organ.ParentSlot is not { } slot)
            return false;

        var oldParent = CompOrNull<BodyPartComponent>(organ.ParentSlot.Parent);

        slot.Child = null;
        organ.ParentSlot = null;
        organ.Body = null;

        if (Containers.TryGetContainer(slot.Parent, BodyContainerId, out var container))
            container.Remove(organId.Value);

        if (TryComp(organId, out TransformComponent? transform))
            transform.AttachToGridOrMap();

        organ.Owner.RandomOffset(0.25f);

        if (oldParent == null)
            return true;

        if (oldParent.Body != null)
        {
            RaiseLocalEvent(organId.Value, new RemovedFromPartInBodyEvent(oldParent.Body.Value, oldParent.Owner));
        }
        else
        {
            RaiseLocalEvent(organId.Value, new RemovedFromPartEvent(oldParent.Owner));
        }

        return true;
    }

    public bool DropOrganAt(EntityUid? organId, EntityCoordinates dropAt, OrganComponent? organ = null)
    {
        if (organId == null || !DropOrgan(organId, organ))
            return false;

        if (TryComp(organId.Value, out TransformComponent? transform))
            transform.Coordinates = dropAt;

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
