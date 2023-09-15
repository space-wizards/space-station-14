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
        if (body.Prototype == null || body.RootPart.ContainedEntity == null)
            return;
        var prototype = Prototypes.Index<BodyPrototype>(body.Prototype);
            InitBody(bodyId, body, prototype);
    }

    protected void InitBody(EntityUid bodyEntity, BodyComponent body, BodyPrototype prototype)
    {
        var root = prototype.Slots[prototype.Root];
        body.RootPart = Containers.EnsureContainer<ContainerSlot>(bodyEntity, BodySlotContainerId);
        if (root.Part == null)
            return;
        var rootPartEntity =  Spawn(root.Part, bodyEntity.ToCoordinates());
        var partComponent = Comp<BodyPartComponent>(rootPartEntity);
        partComponent.Body = bodyEntity;
        InitPartsLegacy(rootPartEntity, partComponent, prototype, prototype.Root);
    }

    protected void InitPart(EntityUid bodyPartId, BodyPartComponent bodyPart, EntityUid? parentBodyPartId,
        BodyPartComponent? parentBodypart, EntityUid? owningBodyId, params (string, BodyPartType, EntityUid?)[] slots)
    {
        bodyPart.Parent = parentBodyPartId;
        bodyPart.Body = owningBodyId;
        foreach (var (slotId, slotPartType, slotBodyPartEntity) in slots)
        {
            var slotContainer = _container.EnsureContainer<ContainerSlot>(bodyPartId, "BodySlot_" + slotId);
            bodyPart.Children.Add(slotId, new BodyPartSlot(slotPartType, slotContainer));
            if (slotBodyPartEntity == null)
                continue;
            slotContainer.Insert(slotBodyPartEntity.Value, EntityManager);
        }
    }


    protected void InitializeParts(EntityUid owningBodyId, EntityUid rootBodyPartId, BodyPartComponent rootBodyPart, string rootSlotId,
        BodyPrototype prototype)
    {
        //TODO make root connections
    }

    protected void InitPartsLegacy(EntityUid rootBodyId, EntityUid parentPartId, BodyPartComponent parentPart, BodyPrototype prototype,
        string slotId, HashSet<string>? initialized = null)
    {
        initialized ??= new HashSet<string>();

        if (initialized.Contains(slotId))
            return;

        initialized.Add(slotId);

        var (_, connections, organs) = prototype.Slots[slotId];
        connections = new HashSet<string>(connections);
        connections.ExceptWith(initialized);

        var coordinates = rootBodyId.ToCoordinates();
        var subConnections = new List<(BodyPartComponent child, string slotId)>();
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
            AttachPart(parentPartId, slot, )
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
            InitPartsLegacy(connection.child, prototype, connection.slotId, initialized);
        }
    }
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(EntityUid? id, BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref body, false) || body.RootPart.ContainedEntity == null ||
            !Resolve(body.RootPart.ContainedEntity.Value, ref rootPart)
            )
            yield break;

        yield return (body.RootPart.ContainedEntity.Value, rootPart);

        foreach (var child in GetPartChildren(body.RootPart.ContainedEntity, rootPart))
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
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false) || body.RootPart.ContainedEntity == null)
            yield break;

        foreach (var slot in GetPartAllSlots(body.RootPart.ContainedEntity))
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

    public static string GetSlotContainerName(string slotName)
    {
        return BodyPartSlotPrefix + slotName;
    }
}
