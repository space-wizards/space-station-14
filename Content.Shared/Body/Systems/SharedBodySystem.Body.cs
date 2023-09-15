using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using MapInitEvent = Robust.Shared.GameObjects.MapInitEvent;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    [Dependency] private readonly INetManager _netManager = default!;

    private const string BodyPartSlotPrefix = "BodySlot_";

    public void InitializeBody()
    {
        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnBodyMapInit);
        SubscribeLocalEvent<BodyComponent, CanDragEvent>(OnBodyCanDrag);
    }



    private void OnBodyCanDrag(EntityUid uid, BodyComponent component, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnBodyMapInit(EntityUid bodyId, BodyComponent body, MapInitEvent args)
    {
        if (body.Prototype == null || body.RootContainer.ContainedEntity == null)
            return;
        var prototype = Prototypes.Index<BodyPrototype>(body.Prototype);
            InitBody(bodyId, body, prototype);
    }

    protected void InitBody(EntityUid bodyEntity, BodyComponent body, BodyPrototype prototype)
    {
        var protoRoot = prototype.Slots[prototype.Root];
        body.RootContainer = Containers.EnsureContainer<ContainerSlot>(bodyEntity, BodySlotContainerId);
        if (protoRoot.Part == null)
            return;
        var rootPartEntity =  Spawn(protoRoot.Part, bodyEntity.ToCoordinates());
        var rootPart = Comp<BodyPartComponent>(rootPartEntity);
        rootPart.Body = bodyEntity;
        InitParts(bodyEntity, rootPartEntity, rootPart, prototype);
    }

    protected void InitParts(EntityUid rootBodyId, EntityUid parentPartId, BodyPartComponent parentPart, BodyPrototype prototype,
        HashSet<string>? initialized = null)
    {
        initialized ??= new HashSet<string>();

        if (parentPart.SlotId == null || initialized.Contains(parentPart.SlotId))
            return;

        initialized.Add(parentPart.SlotId);

        var (_, connections, organs) = prototype.Slots[parentPart.SlotId];
        connections = new HashSet<string>(connections);
        connections.ExceptWith(initialized);

        var coordinates = rootBodyId.ToCoordinates();
        var subConnections = new List<(EntityUid childid,BodyPartComponent child, string slotId)>();
        foreach (var connection in connections)
        {
            var childSlot = prototype.Slots[connection];
            if (childSlot.Part == null)
                continue;

            var childPart = Spawn(childSlot.Part, coordinates);
            var childPartComponent = Comp<BodyPartComponent>(childPart);
            var slot = CreatePartSlot(parentPartId, connection, childPartComponent.PartType, parentPart);
            if (slot == null)
            {
                Logger.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                continue;
            }

            AttachPart(parentPartId, slot, childPart, parentPart, childPartComponent);
            subConnections.Add((childPart,childPartComponent, connection));
        }

        foreach (var (organSlotId, organId) in organs)
        {
            var organ = Spawn(organId, coordinates);
            var organComponent = Comp<OrganComponent>(organ);

            var slot = CreateOrganSlot(organSlotId, parentPartId, parentPart);
            if (slot == null)
            {
                Logger.Error($"Could not create slot for connection {organSlotId} in body {prototype.ID}");
                continue;
            }

            InsertOrgan(rootBodyId,organ, parentPart, organComponent);
        }

        foreach (var connection in subConnections)
        {
            InitParts(rootBodyId,connection.childid, connection.child, prototype, initialized);
        }
    }
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(EntityUid? id, BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref body, false) || body.RootContainer.ContainedEntity == null ||
            !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart)
            )
            yield break;

        yield return (body.RootContainer.ContainedEntity.Value, rootPart);

        foreach (var child in GetPartChildren(body.RootContainer.ContainedEntity, rootPart))
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
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false) || body.RootContainer.ContainedEntity == null)
            yield break;

        foreach (var slot in GetPartAllSlots(body.RootContainer.ContainedEntity))
        {
            yield return slot;
        }
    }

    /// <summary>
    /// Returns all body part slots in the graph, including ones connected by
    /// body parts which are null.
    /// </summary>
    /// <param name="partId"></param>
    /// <param name="part"></param>
    /// <returns></returns>
    public IEnumerable<BodyPartSlot> GetAllBodyPartSlots(EntityUid partId, BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, false))
            yield break;

        foreach (var (slotId,slot) in part.Children)
        {
            if (!TryComp<BodyPartComponent>(slot.Entity, out var childPart))
                continue;

            yield return slot;

            foreach (var childData in GetAllBodyPartSlots(slot.Entity.Value, childPart))
            {
                yield return childData;
            }
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
