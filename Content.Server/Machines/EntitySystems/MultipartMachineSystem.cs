using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Machines.Components;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultipartMachineComponent, ComponentStartup>(OnComponentStartup);
    }

    /// <summary>
    /// Handles resolving the component type to a registration we can use for validating
    /// the machine parts we find.
    /// </summary>
    /// <param name="ent">Entity/Component that just started</param
    /// <param name="args">Args for the startup</param>
    private void OnComponentStartup(Entity<MultipartMachineComponent> ent, ref ComponentStartup args)
    {
        foreach (var (name, part) in ent.Comp.Parts)
        {
            if (!_factory.TryGetRegistration(part.Component, out var registration))
            {
                throw new Exception($"Unable to resolve component type [{part.Component}] for machine part [{name}]");
            }
        }
    }

    /// <summary>
    /// Convenience method for getting a specific part of the machine by name.
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query</param>
    /// <param name="partName">Name of the part to find, must match the name specified in YAML</param>
    /// <returns>May contain the resoilved EntityUid for the specified part, null otherwise</returns>
    public EntityUid? GetPartEntity(Entity<MultipartMachineComponent?> ent, string partName)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        if (ent.Comp.Parts.TryGetValue(partName, out var value))
        {
            return GetEntity(value.Entity);
        }

        return null;
    }

    /// <summary>
    /// Convenience method for getting a specific part of the machine by name.
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query</param>
    /// <param name="partName">Name of the part to find, must match the name specified in YAML</param>
    /// <param name="entity">Out var which may contain the matched EntityUid for the specified part</param>
    /// <returns>True if the part is found and has an matched entity, false otherwise</returns>
    public bool TryGetPartEntity(Entity<MultipartMachineComponent?> ent, string partName, [NotNullWhen(true)] out EntityUid? entity)
    {
        entity = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Parts.TryGetValue(partName, out var value))
        {
            return TryGetEntity(value.Entity, out entity);
        }

        return false;
    }

    /// <summary>
    /// Scans the specified coordinates for any anchored entities that might match the given
    /// component and rotation requirements.
    /// </summary>
    /// <param name="machineOrigin">Origin coordinates for the machine</param>
    /// <param name="rotation">Rotation we're expecting to use to </param>
    /// <param name="query">Entity query for the specific component the entity must have</param>
    /// <param name="gridUid">EntityUID of the grid to use for the lookup</param>
    /// <param name="grid">Grid to use for the lookup</param>
    /// <param name="part">Part we're searching for</param>
    /// <returns>True when part is found and matches, false otherwise</returns>
    private bool ScanPart(
        Vector2i machineOrigin,
        Angle rotation,
        EntityQuery<IComponent> query,
        EntityUid gridUid,
        MapGridComponent grid,
        ref MachinePart part)
    {
        // Safety first, nuke any existing data
        part.Entity = null;

        var expectedLocation = machineOrigin + part.Offset.Rotate(rotation);
        var expectedRotation = part.Rotation + rotation;

        foreach (var entity in _mapSystem.GetAnchoredEntities(gridUid, grid, expectedLocation))
        {
            if (query.TryGetComponent(entity, out var comp) &&
                Transform(entity).LocalRotation.EqualsApprox(expectedRotation.Theta))
            {
                part.Entity = GetNetEntity(entity);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs a rescan of all parts of the machine to confirm they exist and match
    /// the specified requirments for offset, rotation, and components.
    /// </summary>
    /// <param name="ent">Entity to rescan for</param>
    /// <returns>True if all parts are found and match, false otherwise</returns>
    public bool Rescan(Entity<MultipartMachineComponent> ent)
    {
        // Get all required transform information to start looking for the other parts based on their offset
        var xformQuery = GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(ent.Owner, out var xform) || !xform.Anchored)
        {
            return false;
        }

        var gridUid = xform.GridUid;
        if (gridUid == null || gridUid != xform.ParentUid || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return false;
        }

        // Whichever component has the MultipartMachine component should be treated as the origin
        var machineOrigin = _mapSystem.TileIndicesFor(gridUid!.Value, grid, xform.Coordinates);

        var missingParts = false;
        for (var i = 0; i < ent.Comp.Parts.Count; ++i)
        {
            var part = ent.Comp.Parts.Values.ElementAt(i);
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
                ScanPart(machineOrigin, ent.Comp.Rotation.Value, query, gridUid.Value, grid, ref part);
            }
            else
            {
                // We have NO idea where our parts could be orientated so we'll have to iterate through 360 degrees
                // to try and find a match
                Angle curAngle = 0;
                for (var j = 0; j < 4; ++j)
                {
                    if (ScanPart(machineOrigin, curAngle, query, gridUid.Value, grid, ref part))
                    {
                        // This entity succeeds, store the direction we used to get this one and expect all
                        // future machine parts to match this direction.
                        ent.Comp.Rotation = curAngle;
                        break;
                    }

                    curAngle += Math.PI / 2;
                }
            }

            if (!part.Entity.HasValue)
            {
                missingParts = true;
            }
        }

        Dirty(ent);

        return !missingParts;
    }
}
