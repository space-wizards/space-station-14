using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Body.Systems;

[Virtual]
public class SharedBodySystem : EntitySystem
{
    private const string BodyContainerId = "BodyContainer";
    private const string PartContainerId = "BodyPartContainer";
    private const string OrganContainerId = "BodyOrganContainer";

    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBodyComponent, ComponentRemove>(OnBodyRemoved);
        SubscribeLocalEvent<SharedBodyPartComponent, ComponentRemove>(OnPartRemoved);

        SubscribeLocalEvent<SharedBodyPartComponent, ComponentGetState>(OnPartGetState);
        SubscribeLocalEvent<SharedBodyPartComponent, ComponentHandleState>(OnPartHandleState);
    }

    private void OnBodyRemoved(EntityUid uid, SharedBodyComponent body, ComponentRemove args)
    {
        if (body.Root == null)
            return;

        if (TryComp(body.Root, out SharedBodyPartComponent? part))
        {
            part.Parent = null;
            Dirty(part);
        }

        body.Root = null;
        Dirty(body);
    }

    private void OnPartRemoved(EntityUid uid, SharedBodyPartComponent part, ComponentRemove args)
    {
        if (part.Parent == null)
            return;

        if (TryComp(part.Parent, out SharedBodyComponent? body))
        {
            body.Root = null;
            Dirty(body);
        }

        if (TryComp(part.Parent, out SharedBodyPartComponent? parentPart))
        {
            parentPart.Children.Remove(uid);
            Dirty(parentPart);
        }

        part.Parent = null;
        Dirty(part);
    }

    public IEnumerable<SharedBodyPartComponent> GetBodyParts(EntityUid? id, SharedBodyComponent? body = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref body, false) ||
            !TryComp(body.Root, out SharedBodyPartComponent? part))
            yield break;

        yield return part;

        foreach (var childPart in GetPartChildren(body.Root, part))
        {
            yield return childPart;
        }
    }

    public IEnumerable<SharedBodyPartComponent> GetPartChildren(EntityUid? id, SharedBodyPartComponent? part = null)
    {
        if (id == null || !Resolve(id.Value, ref part, false))
            yield break;

        foreach (var child in part.Children)
        {
            if (!TryComp(child, out SharedBodyPartComponent? childPart))
                continue;

            yield return childPart;

            foreach (var subPart in GetPartChildren(child, childPart))
            {
                yield return subPart;
            }
        }
    }

    public IEnumerable<OrganComponent> GetBodyOrgans(EntityUid? id, SharedBodyComponent? body = null)
    {
        foreach (var part in GetBodyParts(id, body))
        {
            foreach (var organ in part.Organs)
            {
                if (TryComp(organ, out OrganComponent? organComponent))
                    yield return organComponent;
            }
        }
    }

    /// <summary>
    ///     Returns a list of ValueTuples of <see cref="T"/> and OrganComponent on each organ
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="body">The body to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    public List<(T Comp, OrganComponent Mech)> GetComponentsOnOrgans<T>(EntityUid uid,
        SharedBodyComponent? body = null) where T : Component
    {
        if (!Resolve(uid, ref body))
            return new List<(T Comp, OrganComponent Mech)>();

        var query = EntityManager.GetEntityQuery<T>();
        var list = new List<(T Comp, OrganComponent Mech)>(3);
        foreach (var organ in GetBodyOrgans(uid, body))
        {
            if (query.TryGetComponent(organ.Owner, out var comp))
                list.Add((comp, organ));
        }

        return list;
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and OrganComponent on each organs
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="body">The body to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetComponentsOnOrgans<T>(EntityUid uid,
        [NotNullWhen(true)] out List<(T Comp, OrganComponent Mech)>? comps,
        SharedBodyComponent? body = null) where T : Component
    {
        if (!Resolve(uid, ref body))
        {
            comps = null;
            return false;
        }

        comps = GetComponentsOnOrgans<T>(uid, body);

        if (comps.Count == 0)
        {
            comps = null;
            return false;
        }

        return true;
    }

    public void AddPartToBody(EntityUid? bodyId, EntityUid? partId, SharedBodyComponent? body = null, SharedBodyPartComponent? part = null)
    {
        // ...

        foreach (var organ in part.Organs)
        {
            RaiseLocalEvent(organ, new AddedToBodyEvent(body), true);
        }
    }

    public void RemovePartFromBody(EntityUid? bodyId, EntityUid? partId, SharedBodyComponent? body = null, SharedBodyPartComponent? part = null)
    {
        // ...

        if (TryComp(partId, out TransformComponent? transform))
        {
            transform.AttachToGridOrMap();
        }

        foreach (var organ in part.Organs)
        {
            RaiseLocalEvent(organ, new RemovedFromBodyEvent(body), true);
        }
    }

    public bool IsPartAttachedToBody(EntityUid? id, [NotNullWhen(true)] out SharedBodyComponent? body,
        SharedBodyPartComponent? part = null)
    {
        body = null;

        if (id == null ||
            !Resolve(id.Value, ref part, false) ||
            part.Parent is not { } parent)
            return false;

        if (TryComp(parent, out body))
            return true;

        return IsPartAttachedToBody(parent, out body);
    }

    public bool AddOrganTo(EntityUid? organId, EntityUid? partId, OrganComponent? organ = null,
        SharedBodyPartComponent? part = null)
    {
        if (organId == null ||
            !Resolve(organId.Value, ref organ, false) ||
            partId == null ||
            !Resolve(partId.Value, ref part, false))
            return false;

        if (organ.Part != null)
            OrphanOrgan(organId, organ);

        organ.Part = partId;
        part.Organs.Add(organId.Value);

        Dirty(organ);
        Dirty(part);

        if (IsPartAttachedToBody(partId, out var body, part))
        {
            RaiseLocalEvent(organId.Value, new AddedToPartInBodyEvent(body, part));
        }
        else
        {
            RaiseLocalEvent(organId.Value, new AddedToPartEvent(part));
        }

        return true;
    }

    public bool OrphanOrgan(EntityUid? id, OrganComponent? organ = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref organ, false) ||
            organ.Part == null)
            return false;

        if (_containers.TryGetContainer(organ.Part.Value, PartContainerId, out var container))
        {
            container.Remove(id.Value);
        }

        if (TryComp(organ.Part, out SharedBodyPartComponent? part))
        {
            if (!part.Organs.Remove(id.Value))
                return false;

            Dirty(part);
        }

        var oldOrganPart = organ.Part.Value;
        organ.Part = null;
        Dirty(organ);

        if (part != null)
        {
            if (IsPartAttachedToBody(oldOrganPart, out var body, part))
            {
                RaiseLocalEvent(oldOrganPart, new RemovedFromPartInBodyEvent(body, part));
            }
            else
            {
                RaiseLocalEvent(oldOrganPart, new RemovedFromPartEvent(part));
            }
        }

        return true;
    }

    public bool OrphanOrgan(EntityUid? id, EntityCoordinates dropAt, OrganComponent? organ = null)
    {
        if (id == null || !OrphanOrgan(id, organ))
            return false;

        Transform(id.Value).Coordinates = dropAt;
        return true;
    }

    public HashSet<EntityUid> GibPart(EntityUid? id, SharedBodyPartComponent? part = null)
    {
        var gibs = new HashSet<EntityUid>();

        if (id == null || !Resolve(id.Value, ref part, false))
            return gibs;

        foreach (var organ in part.Organs)
        {
            if (OrphanOrgan(organ))
            {
                gibs.Add(organ);
            }
        }

        return gibs;
    }

    private void OnPartGetState(EntityUid uid, SharedBodyPartComponent part, ref ComponentGetState args)
    {
        args.State = new BodyPartComponentState(new HashSet<EntityUid>(part.Organs));
    }

    private void OnPartHandleState(EntityUid uid, SharedBodyPartComponent part, ref ComponentHandleState args)
    {
        if (args.Current is not BodyPartComponentState state)
            return;

        foreach (var organ in part.Organs.ToArray())
        {
            if (!state.Organs.Contains(organ))
                OrphanOrgan(organ);
        }

        foreach (var organ in state.Organs)
        {
            if (!part.Organs.Contains(organ))
                AddOrganTo(organ, part.Owner, null, part);
        }
    }
}
