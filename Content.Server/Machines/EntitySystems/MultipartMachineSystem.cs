using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Machines.Components;
using Content.Shared.Machines.Components;
using Content.Shared.Machines.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Machines.EntitySystems;

/// <summary>
/// Server side handling of multipart machines.
/// When requested, performs scans of the map area around the specified entity
/// to find and match parts of the machine.
/// </summary>
public sealed class MultipartMachineSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultipartMachineComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<ConstructionComponent, AfterConstructionChangeEntityEvent>(OnConstructionNodeChanged);
        SubscribeLocalEvent<ConstructionComponent, AnchorStateChangedEvent>(OnConstructionAnchorChanged);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    /// Handles resolving the component type to a registration we can use for validating
    /// the machine parts we find.
    /// </summary>
    /// <param name="ent">Entity/Component that just started.</param>
    /// <param name="args">Args for the startup.</param>
    private void OnComponentStartup(Entity<MultipartMachineComponent> ent, ref ComponentStartup args)
    {
        foreach (var (name, part) in ent.Comp.Parts)
        {
            if (!_factory.TryGetRegistration(part.Component, out var registration))
                throw new Exception($"Unable to resolve component type [{part.Component}] for machine part [{name}]");
        }
    }

    /// <summary>
    /// Handles when a constructable entity has been created due to a move in a construction graph.
    /// Scans all known multipart machines and rescans any that have a part which matches that specific graph
    /// and node IDs.
    /// </summary>
    /// <param name="ent">Constructable entity that has moved in a graph.</param>
    /// <param name="args">Args for this event.</param>
    private void OnConstructionNodeChanged(Entity<ConstructionComponent> ent,
        ref AfterConstructionChangeEntityEvent args)
    {
        var query = EntityQueryEnumerator<MultipartMachineComponent>();
        while (query.MoveNext(out var uid, out var machine))
        {
            foreach (var part in machine.Parts.Values)
            {
                if (args.Graph == part.Graph &&
                    (args.PreviousNode == part.ExpectedNode || args.CurrentNode == part.ExpectedNode))
                {
                    Rescan((uid, machine));
                    break; // No need to scan the same machine again
                }
            }
        }
    }

    /// <summary>
    /// Handles when a constructable entity has been anchored or unanchored by a user.
    /// We might be able to link an unanchored part to a machine, but anchoring a constructable
    /// entity will require a rescan of all machines as we have no idea what machine it might be a
    /// part of.
    /// </summary>
    /// <param name="ent">Constructable entity that has been anchored or unanchored.</param>
    /// <param name="args">Args for this event, notably the anchor status.</param>
    private void OnConstructionAnchorChanged(Entity<ConstructionComponent> ent, ref AnchorStateChangedEvent args)
    {
        var query = EntityQueryEnumerator<MultipartMachineComponent>();
        while (query.MoveNext(out var uid, out var machine))
        {
            if (!args.Anchored)
            {
                // Some construction is being unanchored, check if its a known part for us
                foreach (var part in machine.Parts.Values)
                {
                    if (part.Entity.HasValue && GetEntity(part.Entity) == ent.Owner)
                    {
                        Rescan((uid, machine));
                        return; // Can just early out now that we have scanned the exact right machine.
                    }
                }
            }
            else
            {
                // We're anchoring some construction, we have no idea which machine this might be for
                // so we have to just check everyone and perform a rescan.
                Rescan((uid, machine));
            }
        }
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

        if (ent.Comp.Parts.TryGetValue(part, out var value))
            value.Entity = null;
    }

    /// <summary>
    /// Returns whether each non-optional part of the machine has a matched entity
    /// </summary>
    /// <param name="ent">Entity to check the assembled state of.</param>
    /// <returns>True if all non-optional parts have a matching entity, false otherwise.</returns>
    public bool IsAssembled(Entity<MultipartMachineComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        foreach (var part in ent.Comp.Parts.Values)
        {
            if (!part.Entity.HasValue && !part.Optional)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Get the EntityUid for the entity bound to a specific part, if one exists.
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query.</param>
    /// <param name="part">Enum value for the part to find, must match the value specified in YAML.</param>
    /// <returns>May contain the resolved EntityUid for the specified part, null otherwise.</returns>
    public EntityUid? GetPartEntity(Entity<MultipartMachineComponent?> ent, Enum part)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        if (ent.Comp.Parts.TryGetValue(part, out var value))
            return GetEntity(value.Entity);

        return null;
    }

    /// <summary>
    /// Get the EntityUid for the entity bound to a specific part, if one exists.
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query.</param>
    /// <param name="part">Enum for the part to find, must match the value specified in YAML.</param>
    /// <param name="entity">Out var which may contain the matched EntityUid for the specified part.</param>
    /// <returns>True if the part is found and has a matched entity, false otherwise.</returns>
    public bool TryGetPartEntity(Entity<MultipartMachineComponent?> ent,
        Enum part,
        [NotNullWhen(true)] out EntityUid? entity)
    {
        entity = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Parts.TryGetValue(part, out var value))
            return TryGetEntity(value.Entity, out entity);

        return false;
    }

    /// <summary>
    /// Check if a machine has an entity bound to a specific part
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query.</param>
    /// <param name="part">Enum for the part to find.</param>
    /// <returns>True if the specific part has a entity bound to it, false otherwise.</returns>
    public bool HasPart(Entity<MultipartMachineComponent?> ent, Enum part)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Parts.TryGetValue(part, out var value))
            return false;

        return value.Entity != null;
    }

    /// <summary>
    /// Scans the specified coordinates for any anchored entities that might match the given
    /// component and rotation requirements.
    /// </summary>
    /// <param name="machineOrigin">Origin coordinates for the machine.</param>
    /// <param name="rotation">Rotation we're expecting to use to.</param>
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

            part.Entity = GetNetEntity(entity);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs a rescan of all parts of the machine to confirm they exist and match
    /// the specified requirements for offset, rotation, and components.
    /// </summary>
    /// <param name="ent">Entity to rescan for.</param>
    /// <param name="user">Optional user entity which has caused this rescan.</param>
    /// <returns>True if all non-optional parts are found and match requirements, false otherwise.</returns>
    public bool Rescan(Entity<MultipartMachineComponent> ent, EntityUid? user = null)
    {
        // Get all required transform information to start looking for the other parts based on their offset
        if (!_xformQuery.TryGetComponent(ent.Owner, out var xform) || !xform.Anchored)
            return false;

        var gridUid = xform.GridUid;
        if (gridUid == null || gridUid != xform.ParentUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        ent.Comp.Rotation = null; // Ensure we reset our expected orientation

        // Whichever component has the MultipartMachine component should be treated as the origin
        var machineOrigin = _mapSystem.TileIndicesFor(gridUid!.Value, grid, xform.Coordinates);

        // Set to true if any of the parts' state changes
        var stateHasChanged = false;

        var missingParts = false;
        for (var i = 0; i < ent.Comp.Parts.Count; ++i)
        {
            var part = ent.Comp.Parts.Values.ElementAt(i);
            var originalPart = part.Entity;
            part.Entity = null;

            if (!_factory.TryGetRegistration(part.Component, out var registration))
                break;

            var query = _entManager.GetEntityQuery(registration.Type);
            if (ent.Comp.Rotation.HasValue)
            {
                // We have already found some entity that roughly matchesz, so we can
                // use that direction for future lookups.
                // Not using this means the orientations of the parts could be wildly different and still
                // "Match" the expected offsets
                ScanPart(machineOrigin, ent.Comp.Rotation.Value, query, gridUid.Value, grid, part);
            }
            else
            {
                // We have NO idea where our parts could be orientated so we'll have to iterate through 360 degrees
                // to try and find a match
                Angle curAngle = 0;
                for (var j = 0; j < 4; ++j)
                {
                    if (ScanPart(machineOrigin, curAngle, query, gridUid.Value, grid, part))
                    {
                        // This entity succeeds, store the direction we used to get this one and expect all
                        // future machine parts to match this direction.
                        ent.Comp.Rotation = curAngle;
                        break;
                    }

                    curAngle += Math.PI / 2;
                }
            }

            if (!part.Entity.HasValue && !part.Optional)
                missingParts = true;

            // Even optional parts should trigger state updates
            if (part.Entity != originalPart)
            {
                if (part.Entity.HasValue)
                {
                    // This part gained an entity, add the Part component so it can find out which machine
                    // it's a part of
                    var comp = EnsureComp<MultipartMachinePartComponent>(GetEntity(part.Entity.Value));
                    comp.Master = ent;
                }
                else
                {
                    // This part lost its entity, ensure we clean up the old entity so it's no longer marked
                    // as something we care about.
                    RemComp<MultipartMachinePartComponent>(GetEntity(originalPart!.Value));
                }

                stateHasChanged = true;
            }
        }

        ent.Comp.IsAssembled = !missingParts;
        if (stateHasChanged)
        {
            var ev = new MultipartMachineAssemblyStateChanged(ent, ent.Comp.IsAssembled, user);
            RaiseLocalEvent(ent, ev);
        }

        Dirty(ent);

        return !missingParts;
    }
}
