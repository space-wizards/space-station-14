using System.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Adds an entity with a DeltaPressureComponent to the DeltaPressure processing list.
    /// Also fills in important information on the component itself.
    /// </summary>
    /// <param name="grid">The grid to add the entity to.</param>
    /// <param name="ent">The entity to add.</param>
    /// <returns>True if the entity was added to the list, false if it could not be added or
    /// if the entity was already present in the list.</returns>
    [PublicAPI]
    public bool TryAddDeltaPressureEntity(Entity<GridAtmosphereComponent?> grid, Entity<DeltaPressureComponent> ent)
    {
        // The entity needs to be part of a grid, and it should be the right one :)
        var xform = Transform(ent);

        // The entity is not on a grid, so it cannot possibly have an atmosphere that affects it.
        if (xform.GridUid == null)
        {
            return false;
        }

        // Entity should be on the grid it's being added to.
        Debug.Assert(xform.GridUid == grid.Owner);

        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (grid.Comp.DeltaPressureEntityLookup.ContainsKey(ent.Owner))
        {
            return false;
        }

        grid.Comp.DeltaPressureEntityLookup[ent.Owner] = grid.Comp.DeltaPressureEntities.Count;
        grid.Comp.DeltaPressureEntities.Add(ent);

        ent.Comp.GridUid = grid.Owner;
        ent.Comp.InProcessingList = true;

        return true;
    }

    /// <summary>
    /// Removes an entity with a DeltaPressureComponent from the DeltaPressure processing list.
    /// </summary>
    /// <param name="grid">The grid to remove the entity from.</param>
    /// <param name="ent">The entity to remove.</param>
    /// <returns>True if the entity was removed from the list, false if it could not be removed or
    /// if the entity was not present in the list.</returns>
    [PublicAPI]
    public bool TryRemoveDeltaPressureEntity(Entity<GridAtmosphereComponent?> grid, Entity<DeltaPressureComponent> ent)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (!grid.Comp.DeltaPressureEntityLookup.TryGetValue(ent.Owner, out var index))
            return false;

        var lastIndex = grid.Comp.DeltaPressureEntities.Count - 1;
        if (lastIndex < 0)
            return false;

        if (index != lastIndex)
        {
            var lastEnt = grid.Comp.DeltaPressureEntities[lastIndex];
            grid.Comp.DeltaPressureEntities[index] = lastEnt;
            grid.Comp.DeltaPressureEntityLookup[lastEnt.Owner] = index;
        }

        grid.Comp.DeltaPressureEntities.RemoveAt(lastIndex);
        grid.Comp.DeltaPressureEntityLookup.Remove(ent.Owner);

        if (grid.Comp.DeltaPressureCursor > grid.Comp.DeltaPressureEntities.Count)
            grid.Comp.DeltaPressureCursor = grid.Comp.DeltaPressureEntities.Count;

        ent.Comp.InProcessingList = false;
        ent.Comp.GridUid = null;
        return true;
    }

    /// <summary>
    /// Checks if a DeltaPressureComponent is currently considered for processing on a grid.
    /// </summary>
    /// <param name="grid">The grid that the entity may belong to.</param>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if the entity is part of the processing list, false otherwise.</returns>
    [PublicAPI]
    public bool IsDeltaPressureEntityInList(Entity<GridAtmosphereComponent?> grid, Entity<DeltaPressureComponent> ent)
    {
        // Dict and list must be in sync - deep-fried if we aren't.
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        var contains = grid.Comp.DeltaPressureEntityLookup.ContainsKey(ent.Owner);
        Debug.Assert(contains == grid.Comp.DeltaPressureEntities.Contains(ent));

        return contains;
    }
}
