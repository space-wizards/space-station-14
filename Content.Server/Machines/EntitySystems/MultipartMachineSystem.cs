using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Machines.Components;
using Content.Shared.Machines.EntitySystems;
using Content.Shared.Machines.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Machines.EntitySystems;

/// <summary>
/// Server side handling of multipart machines.
/// When requested, performs scans of the map area around the specified entity
/// to find and match parts of the machine.
/// </summary>
public sealed class MultipartMachineSystem : SharedMultipartMachineSystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

    // The largest size ANY machine can theoretically have.
    // Used to aid search for machines in range of parts that have been anchored/constructed.
    private const float MaximumRange = 30;
    private readonly HashSet<Entity<MultipartMachineComponent>> _entitiesInRange = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultipartMachineComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<MultipartMachineComponent, AnchorStateChangedEvent>(OnMachineAnchorChanged);

        SubscribeLocalEvent<MultipartMachinePartComponent, AfterConstructionChangeEntityEvent>(OnPartConstructionNodeChanged);
        SubscribeLocalEvent<MultipartMachinePartComponent, AnchorStateChangedEvent>(OnPartAnchorChanged);
    }

    /// <summary>
    /// Clears the matched entity from the specified part
    /// </summary>
    /// <param name="ent">Entity to clear the part for.</param>
    /// <param name="part">Enum value for the part to clear.</param>
    public void ClearPartEntity(Entity<MultipartMachineComponent?> ent, Enum part)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Parts.TryGetValue(part, out var value))
            return;

        if (!value.Entity.HasValue)
            return;

        var partEnt = value.Entity.Value;
        var partComp = EnsureComp<MultipartMachinePartComponent>(partEnt);

        if (partComp.Master.HasValue)
        {
            partComp.Master = null;
            Dirty(partEnt, partComp);
        }

        value.Entity = null;
        Dirty(ent);
    }

    /// <summary>
    /// Performs a rescan of all parts of the machine to confirm they exist and match
    /// the specified requirements for offset, rotation, and components.
    /// </summary>
    /// <param name="ent">Entity to rescan for.</param>
    /// <param name="user">Optional user entity which has caused this rescan.</param>
    /// <returns>True the state of the machine's assembly has changed, false otherwise.</returns>
    public bool Rescan(Entity<MultipartMachineComponent> ent, EntityUid? user = null)
    {
        // Get all required transform information to start looking for the other parts based on their offset
        if (!XformQuery.TryGetComponent(ent.Owner, out var xform) || !xform.Anchored)
            return false;

        var gridUid = xform.GridUid;
        if (gridUid == null || gridUid != xform.ParentUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        // Whichever component has the MultipartMachine component should be treated as the origin
        var machineOrigin = _mapSystem.TileIndicesFor(gridUid.Value, grid, xform.Coordinates);

        // Set to true if any of the parts' state changes
        var stateHasChanged = false;

        // Keep a track of what parts were added or removed so we can inform listeners
        Dictionary<Enum, EntityUid> partsAdded = [];
        Dictionary<Enum, EntityUid> partsRemoved = [];

        var missingParts = false;
        var machineRotation = xform.LocalRotation.GetCardinalDir().ToAngle();
        foreach (var (key, part) in ent.Comp.Parts)
        {
            var originalPart = part.Entity;
            part.Entity = null;

            if (!_factory.TryGetRegistration(part.Component, out var registration))
                break;

            var query = EntityManager.GetEntityQuery(registration.Type);

            ScanPart(machineOrigin, machineRotation, query, gridUid.Value, grid, part);

            if (!part.Entity.HasValue && !part.Optional)
                missingParts = true;

            if (part.Entity == originalPart)
                continue; // Nothing has changed here

            stateHasChanged = true;

            MultipartMachinePartComponent comp;
            EntityUid partEnt;
            if (part.Entity.HasValue)
            {
                // This part gained an entity, add the Part component so it can find out which machine
                // it's a part of
                partEnt = part.Entity.Value;
                comp = EnsureComp<MultipartMachinePartComponent>(partEnt);
                comp.Master = ent;
                partsAdded.Add(key, partEnt);
            }
            else
            {
                // This part lost its entity, ensure we clean up the old entity so it's no longer marked
                // as something we care about.
                partEnt = originalPart!.Value;
                comp = EnsureComp<MultipartMachinePartComponent>(partEnt);
                comp.Master = null;
                partsRemoved.Add(key, partEnt);
            }

            Dirty(partEnt, comp);
        }

        ent.Comp.IsAssembled = !missingParts;
        if (stateHasChanged)
        {
            var ev = new MultipartMachineAssemblyStateChanged(
                ent,
                ent.Comp.IsAssembled,
                user,
                partsAdded,
                partsRemoved
            );
            RaiseLocalEvent(ent, ref ev);

            Dirty(ent);
        }

        return stateHasChanged;
    }

    /// <summary>
    /// Clears all entities bound to parts for a specified machine.
    /// Will also raise the assembly state change and dirty event for it.
    /// </summary>
    /// <param name="ent">Machine to completely clear the parts of.</param>
    private void ClearAllParts(Entity<MultipartMachineComponent> ent)
    {
        var stateHasChanged = false;

        Dictionary<Enum, EntityUid> clearedParts = [];
        foreach (var (key, part) in ent.Comp.Parts)
        {
            if (!part.Entity.HasValue)
                continue;

            stateHasChanged = true;
            var partEntity = part.Entity.Value;
            var partComp = EnsureComp<MultipartMachinePartComponent>(partEntity);

            clearedParts.Add(key, partEntity);

            part.Entity = null;
            part.NetEntity = null;

            partComp.Master = null;
            Dirty(partEntity, partComp);
        }

        ent.Comp.IsAssembled = false;

        if (stateHasChanged)
        {
            var ev = new MultipartMachineAssemblyStateChanged(
                ent,
                ent.Comp.IsAssembled,
                null,
                [],
                clearedParts
            );
            RaiseLocalEvent(ent, ref ev);

            Dirty(ent);
        }
    }

    /// <summary>
    /// Handles any additional setup of the MultipartMachine component.
    /// </summary>
    /// <param name="ent">Entity/Component that just started.</param>
    /// <param name="args">Args for the startup.</param>
    private void OnComponentStartup(Entity<MultipartMachineComponent> ent, ref ComponentStartup args)
    {
        // If anchored, perform a rescan of this machine when the component starts so we can immediately
        // jump to an assembled state if needed.
        if (XformQuery.TryGetComponent(ent.Owner, out var xform) && xform.Anchored)
            Rescan(ent);
    }

    /// <summary>
    /// Handles when a main machine entity has been anchored or unanchored by a user.
    /// Rescanning is then required in order to check whether parts are still in the right places,
    /// and raise a AfterConstructionChangeEntityEvent.
    /// </summary>
    /// <param name="ent">Machine entity that has been anchored or unanchored.</param>
    /// <param name="args">Args for this event.</param>
    private void OnMachineAnchorChanged(Entity<MultipartMachineComponent> ent,
        ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            Rescan(ent);
        else
            ClearAllParts(ent);
    }

    /// <summary>
    /// Handles when a machine part entity has been created due to a move in a construction graph.
    /// Rescans all known multipart machines within range that have a part which matches that specific graph
    /// and node IDs.
    /// </summary>
    /// <param name="ent">Machine part entity that has moved in a graph.</param>
    /// <param name="args">Args for this event.</param>
    private void OnPartConstructionNodeChanged(Entity<MultipartMachinePartComponent> ent,
        ref AfterConstructionChangeEntityEvent args)
    {
        if (!XformQuery.TryGetComponent(ent.Owner, out var constructXform))
            return;

        _lookupSystem.GetEntitiesInRange(constructXform.Coordinates, MaximumRange, _entitiesInRange);
        foreach (var machine in _entitiesInRange)
        {
            foreach (var part in machine.Comp.Parts.Values)
            {
                if (args.Graph == part.Graph &&
                    (args.PreviousNode == part.ExpectedNode || args.CurrentNode == part.ExpectedNode))
                {
                    Rescan(machine);
                    break; // No need to scan the same machine again
                }
            }
        }
    }

    /// <summary>
    /// Handles when a machine part entity has been anchored or unanchored by a user.
    /// We might be able to link an unanchored part to a machine, but anchoring a constructable entity,
    /// which machine parts are, will require a rescan of all machines within range as we have no idea
    /// what machine it might be a part of.
    /// </summary>
    /// <param name="ent">Machine part entity that has been anchored or unanchored.</param>
    /// <param name="args">Args for this event, notably the anchor status.</param>
    private void OnPartAnchorChanged(Entity<MultipartMachinePartComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            if (!TryComp<MultipartMachinePartComponent>(ent.Owner, out var part) || !part.Master.HasValue)
                return; // This is not an entity we care about

            // This is a machine part that is being unanchored, rescan its machine
            if (!TryComp<MultipartMachineComponent>(part.Master, out var machine))
                return;

            Rescan((part.Master.Value, machine));
            return;
        }

        // We're anchoring some construction, we have no idea which machine this might be for
        // so we have to just check everyone in range and perform a rescan.
        if (!XformQuery.TryGetComponent(ent.Owner, out var constructXform))
            return;

        _lookupSystem.GetEntitiesInRange(constructXform.Coordinates, MaximumRange, _entitiesInRange);
        foreach (var machine in _entitiesInRange)
        {
            if (Rescan(machine) && HasPartEntity(machine.AsNullable(), ent.Owner))
                return; // This machine is using this entity so we don't need to go any further
        }
    }

    /// <summary>
    /// Scans the specified coordinates for any anchored entities that might match the given
    /// component and rotation requirements.
    /// </summary>
    /// <param name="machineOrigin">Origin coordinates for the machine.</param>
    /// <param name="rotation">Rotation of the master entity to use when searching for this part.</param>
    /// <param name="query">Entity query for the specific component the entity must have.</param>
    /// <param name="gridUid">EntityUID of the grid to use for the lookup.</param>
    /// <param name="grid">Grid to use for the lookup.</param>
    /// <param name="part">Part we're searching for.</param>
    /// <returns>True when part is found and matches requirements, false otherwise.</returns>
    private bool ScanPart(
        Vector2i machineOrigin,
        Angle rotation,
        EntityQuery<IComponent> query,
        EntityUid gridUid,
        MapGridComponent grid,
        MachinePart part)
    {
        // Safety first, nuke any existing data
        part.Entity = null;

        var expectedLocation = machineOrigin + part.Offset.Rotate(rotation);
        var expectedRotation = part.Rotation + rotation;

        foreach (var entity in _mapSystem.GetAnchoredEntities(gridUid, grid, expectedLocation))
        {
            if (TerminatingOrDeleted(entity))
            {
                // Ignore entities which are in the process of being deleted
                continue;
            }

            if (!query.TryGetComponent(entity, out var comp) ||
                !Transform(entity).LocalRotation.EqualsApprox(expectedRotation.Theta))
            {
                // Either has no transform, or doesn't match the rotation
                continue;
            }

            if (!TryComp<ConstructionComponent>(entity, out var construction) ||
                construction.Graph != part.Graph ||
                construction.Node != part.ExpectedNode)
            {
                // This constructable doesn't match the right graph we expect
                continue;
            }

            part.Entity = entity;
            part.NetEntity = GetNetEntity(entity);
            return true;
        }

        return false;
    }
}
