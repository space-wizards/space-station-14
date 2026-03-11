using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.NodeGroups;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Adds a <see cref="PipeNet"/> to a grid.
    /// </summary>
    /// <param name="grid">The grid to add the pipe net to.</param>
    /// <param name="pipeNet">The pipe net to add.</param>
    /// <returns>True if the pipe net was added, false otherwise.</returns>
    [PublicAPI]
    public bool AddPipeNet(Entity<GridAtmosphereComponent?> grid, PipeNet pipeNet)
    {
        return _atmosQuery.Resolve(grid, ref grid.Comp, false) && grid.Comp.PipeNets.Add(pipeNet);
    }

    /// <summary>
    /// Removes a <see cref="PipeNet"/> from a grid.
    /// </summary>
    /// <param name="grid">The grid to remove the pipe net from.</param>
    /// <param name="pipeNet">The pipe net to remove.</param>
    /// <returns>True if the pipe net was removed, false otherwise.</returns>
    [PublicAPI]
    public bool RemovePipeNet(Entity<GridAtmosphereComponent?> grid, PipeNet pipeNet)
    {
        // Technically this event can be fired even on grids that don't
        // actually have grid atmospheres.
        if (pipeNet.Grid is not null)
        {
            var ev = new PipeNodeGroupRemovedEvent(grid, pipeNet.NetId);
            RaiseLocalEvent(ref ev);
        }

        return _atmosQuery.Resolve(grid, ref grid.Comp, false) && grid.Comp.PipeNets.Remove(pipeNet);
    }
}

/// <summary>
/// Raised broadcasted when a pipe node group within a grid has been removed.
/// </summary>
/// <param name="Grid">The grid with the removed node group.</param>
/// <param name="NetId">The net id of the removed node group.</param>
[ByRefEvent]
public record struct PipeNodeGroupRemovedEvent(EntityUid Grid, int NetId);
