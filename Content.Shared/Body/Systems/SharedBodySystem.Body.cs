using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Coordinates;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    public void InitializeBody()
    {
        SubscribeLocalEvent<BodyComponent, ComponentInit>(OnBodyInit);

        SubscribeLocalEvent<BodyComponent, ComponentGetState>(OnBodyGetState);
        SubscribeLocalEvent<BodyComponent, ComponentHandleState>(OnBodyHandleState);
    }

    private void OnBodyInit(EntityUid bodyId, BodyComponent body, ComponentInit args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (body.Prototype == null || body.Root != null)
            return;

        var prototype = Prototypes.Index<BodyPrototype>(body.Prototype);
        InitBody(body, prototype);
        Dirty(body); // Client doesn't actually spawn the body, need to sync it
    }

    private void OnBodyGetState(EntityUid uid, BodyComponent body, ref ComponentGetState args)
    {
        args.State = new BodyComponentState(body.Root, body.GibSound);
    }

    private void OnBodyHandleState(EntityUid uid, BodyComponent body, ref ComponentHandleState args)
    {
        if (args.Current is not BodyComponentState state)
            return;

        body.Root = state.Root;
        body.GibSound = state.GibSound;
    }

    public bool TryCreateBodyRootSlot(
        EntityUid? bodyId,
        string slotId,
        [NotNullWhen(true)] out BodyPartSlot? slot,
        BodyComponent? body = null)
    {
        slot = null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (bodyId == null ||
            !Resolve(bodyId.Value, ref body, false) ||
            body.Root != null)
            return false;

        slot = new BodyPartSlot(slotId, bodyId.Value, null);
        body.Root = slot;

        return true;
    }

    protected abstract void InitBody(BodyComponent body, BodyPrototype prototype);

    protected void InitPart(BodyPartComponent parent, BodyPrototype prototype, string slotId, HashSet<string>? initialized = null)
    {
        initialized ??= new HashSet<string>();

        if (initialized.Contains(slotId))
            return;

        initialized.Add(slotId);

        var (_, connections, organs) = prototype.Slots[slotId];
        connections = new HashSet<string>(connections);
        connections.ExceptWith(initialized);

        var coordinates = parent.Owner.ToCoordinates();
        var subConnections = new List<(BodyPartComponent child, string slotId)>();

        Containers.EnsureContainer<Container>(parent.Owner, BodyContainerId);

        foreach (var connection in connections)
        {
            var childSlot = prototype.Slots[connection];
            var childPart = Spawn(childSlot.Part, coordinates);
            var childPartComponent = Comp<BodyPartComponent>(childPart);
            var slot = CreatePartSlot(connection, parent.Owner, childPartComponent.PartType, parent);
            if (slot == null)
            {
                Logger.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                continue;
            }

            AttachPart(childPart, slot, childPartComponent);
            subConnections.Add((childPartComponent, connection));
        }

        foreach (var (organSlotId, organId) in organs)
        {
            var organ = Spawn(organId, coordinates);
            var organComponent = Comp<OrganComponent>(organ);

            var slot = CreateOrganSlot(organSlotId, parent.Owner, parent);
            if (slot == null)
            {
                Logger.Error($"Could not create slot for connection {organSlotId} in body {prototype.ID}");
                continue;
            }

            InsertOrgan(organ, slot, organComponent);
        }

        foreach (var connection in subConnections)
        {
            InitPart(connection.child, prototype, connection.slotId, initialized);
        }
    }
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(EntityUid? id, BodyComponent? body = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref body, false) ||
            !TryComp(body.Root?.Child, out BodyPartComponent? part))
            yield break;

        yield return (body.Root.Child.Value, part);

        foreach (var child in GetPartChildren(body.Root.Child))
        {
            yield return child;
        }
    }

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetBodyOrgans(EntityUid? bodyId, BodyComponent? body = null)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false))
            yield break;

        foreach (var part in GetBodyChildren(bodyId, body))
        {
            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                yield return organ;
            }
        }
    }

    public IEnumerable<BodyPartSlot> GetBodyAllSlots(EntityUid? bodyId, BodyComponent? body = null)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false))
            yield break;

        foreach (var slot in GetPartAllSlots(body.Root?.Child))
        {
            yield return slot;
        }
    }

    public virtual HashSet<EntityUid> GibBody(EntityUid? partId, bool gibOrgans = false,
        BodyComponent? body = null, bool deleteItems = false)
    {
        if (partId == null || !Resolve(partId.Value, ref body, false))
            return new HashSet<EntityUid>();

        var parts = GetBodyChildren(partId, body).ToArray();
        var gibs = new HashSet<EntityUid>(parts.Length);

        foreach (var part in parts)
        {
            DropPart(part.Id, part.Component);
            gibs.Add(part.Id);

            if (!gibOrgans)
                continue;

            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                DropOrgan(organ.Id, organ.Component);
                gibs.Add(organ.Id);
            }
        }

        return gibs;
    }
}
