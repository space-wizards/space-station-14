using System.Linq;
using System.Numerics;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.DragDrop;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    /*
     * tl;dr of how bobby works
     * - BodyComponent uses a BodyPrototype as a template.
     * - On MapInit we spawn the root entity in the prototype and spawn all connections outwards from here
     * - Each "connection" is a body part (e.g. arm, hand, etc.) and each part can also contain organs.
     */

    [Dependency] private readonly InventorySystem _inventory = default!;

    private void InitializeBody()
    {
        SubscribeLocalEvent<BodyComponent, ComponentInit>(OnBodyInit);
        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnBodyMapInit);
        SubscribeLocalEvent<BodyComponent, CanDragEvent>(OnBodyCanDrag);
    }

    private void OnBodyInit(EntityUid bodyId, BodyComponent body, ComponentInit args)
    {
        // Setup the initial container.
        body.RootContainer = Containers.EnsureContainer<ContainerSlot>(bodyId, BodyRootContainerId);
    }

    private void OnBodyMapInit(EntityUid bodyId, BodyComponent body, MapInitEvent args)
    {
        if (body.Prototype == null)
            return;

        // One-time setup
        // Obviously can't run in Init to avoid double-spawns on save / load.
        var prototype = Prototypes.Index(body.Prototype.Value);
        MapInitBody(bodyId, prototype);
    }

    private void MapInitBody(EntityUid bodyEntity, BodyPrototype prototype)
    {
        var protoRoot = prototype.Slots[prototype.Root];
        if (protoRoot.Part == null)
            return;

        // This should already handle adding the entity to the root.
        var rootPartEntity = SpawnInContainerOrDrop(protoRoot.Part, bodyEntity, BodyRootContainerId);
        var rootPart = Comp<BodyPartComponent>(rootPartEntity);
        rootPart.Body = bodyEntity;
        Dirty(rootPartEntity, rootPart);

        // Setup the rest of the body entities.
        SetupOrgans(rootPartEntity, rootPart, protoRoot.Organs);
        MapInitParts(rootPartEntity, prototype);
    }

    private void OnBodyCanDrag(EntityUid uid, BodyComponent component, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    /// Sets up all of the relevant body parts for a particular body entity and root part.
    /// </summary>
    private void MapInitParts(EntityUid rootPartId, BodyPrototype prototype)
    {
        // Start at the root part and traverse the body graph, setting up parts as we go.
        // Basic BFS pathfind.
        var rootSlot = prototype.Root;
        var frontier = new Queue<string>();
        frontier.Enqueue(rootSlot);

        // Child -> Parent connection.
        var cameFrom = new Dictionary<string, string>();
        cameFrom[rootSlot] = rootSlot;
        // Maps slot to its relevant entity.
        var cameFromEntities = new Dictionary<string, EntityUid>();
        cameFromEntities[rootSlot] = rootPartId;

        while (frontier.TryDequeue(out var currentSlotId))
        {
            var currentSlot = prototype.Slots[currentSlotId];

            foreach (var connection in currentSlot.Connections)
            {
                // Already been handled
                if (!cameFrom.TryAdd(connection, currentSlotId))
                    continue;

                // Setup part
                var connectionSlot = prototype.Slots[connection];
                var parentEntity = cameFromEntities[currentSlotId];
                var parentPartComponent = Comp<BodyPartComponent>(parentEntity);

                // Spawn the entity on the target
                // then get the body part type, create the slot, and finally
                // we can insert it into the container.
                var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                cameFromEntities[connection] = childPart;

                var childPartComponent = Comp<BodyPartComponent>(childPart);
                var partSlot = CreatePartSlot(parentEntity, connection, childPartComponent.PartType, parentPartComponent);
                var cont = Containers.GetContainer(parentEntity, GetPartSlotContainerId(connection));

                if (partSlot == null || !cont.Insert(childPart))
                {
                    Log.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                    QueueDel(childPart);
                    continue;
                }

                // Add organs
                SetupOrgans(childPart, childPartComponent, connectionSlot.Organs);

                // Enqueue it so we can also get its neighbors.
                frontier.Enqueue(connection);
            }
        }
    }

    private void SetupOrgans(EntityUid partId, BodyPartComponent partComponent, Dictionary<string, string> organs)
    {
        foreach (var (organSlotId, organProto) in organs)
        {
            var slot = CreateOrganSlot(organSlotId, partId, partComponent);
            SpawnInContainerOrDrop(organProto, partId, GetOrganContainerId(organSlotId));

            if (slot == null)
            {
                Log.Error($"Could not create organ for slot {organSlotId} in {ToPrettyString(partId)}");
            }
        }
    }

    /// <summary>
    /// Gets all body containers on this entity including the root one.
    /// </summary>
    public IEnumerable<BaseContainer> GetBodyContainers(EntityUid id, BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (!Resolve(id, ref body, false) ||
            body.RootContainer.ContainedEntity == null ||
            !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        yield return body.RootContainer;

        foreach (var childContainer in GetPartContainers(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            yield return childContainer;
        }
    }

    /// <summary>
    /// Gets all child body parts of this entity, including the root entity.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(EntityUid? id, BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (id == null ||
            !Resolve(id.Value, ref body, false) ||
            body.RootContainer.ContainedEntity == null ||
            !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        yield return (body.RootContainer.ContainedEntity.Value, rootPart);

        foreach (var child in GetBodyPartChildren(body.RootContainer.ContainedEntity.Value, rootPart))
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

    /// <summary>
    /// Returns all body part slots for this entity.
    /// </summary>
    /// <param name="bodyId"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public IEnumerable<BodyPartSlot> GetBodyAllSlots(EntityUid bodyId, BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body, false) || body.RootContainer.ContainedEntity == null)
            yield break;

        foreach (var slot in GetAllBodyPartSlots(body.RootContainer.ContainedEntity.Value))
        {
            yield return slot;
        }
    }

    public virtual HashSet<EntityUid> GibBody(EntityUid bodyId, bool gibOrgans = false,
        BodyComponent? body = null, bool deleteItems = false, bool deleteBrain = false)
    {
        var gibs = new HashSet<EntityUid>();

        if (!Resolve(bodyId, ref body, false))
            return gibs;

        var parts = GetBodyChildren(bodyId, body).ToArray();
        gibs.EnsureCapacity(parts.Length);

        foreach (var part in parts)
        {
            SharedTransform.AttachToGridOrMap(part.Id);
            gibs.Add(part.Id);

            if (!gibOrgans)
                continue;

            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                SharedTransform.AttachToGridOrMap(organ.Id);
                gibs.Add(organ.Id);
            }
        }
        if(TryComp<InventoryComponent>(bodyId, out var inventory))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(bodyId))
            {
                SharedTransform.AttachToGridOrMap(item);
                gibs.Add(item);
            }
        }
        return gibs;
    }
}
