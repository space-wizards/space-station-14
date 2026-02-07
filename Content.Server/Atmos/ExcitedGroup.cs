namespace Content.Server.Atmos;

/// <summary>
/// <para>Internal Atmospherics class that stores data about a group of <see cref="TileAtmosphere"/>s
/// that are excited and need to be processed.</para>
///
/// <para>Excited Groups is an optimization routine executed during LINDA
/// that bunches small groups of active <see cref="TileAtmosphere"/>s
/// together and performs equalization processing on the entire group when the group dissolves.
/// Dissolution happens when LINDA operations between the tiles decrease to very low mole deltas.</para>
/// </summary>
public sealed class ExcitedGroup
{
    /// <summary>
    /// Whether this Active Group has been disposed of.
    /// Used to make sure we don't perform operations on active groups that
    /// we've already dissolved.
    /// </summary>
    [ViewVariables]
    public bool Disposed = false;

    /// <summary>
    /// List of tiles that belong to this excited group.
    /// </summary>
    [ViewVariables]
    public readonly List<TileAtmosphere> Tiles = new(100);

    /// <summary>
    /// Cycles before this excited group will be queued for dismantling.
    /// Dismantling is the process of equalizing the atmosphere
    /// across all tiles in the excited group and removing the group.
    /// </summary>
    [ViewVariables]
    public int DismantleCooldown = 0;

    /// <summary>
    /// Cycles before this excited group will be allowed to break down and deactivate.
    /// Breakdown occurs when the excited group is small enough and inactive enough
    /// to be safely removed without equalization. Used where the mole deltas across
    /// the group are very low but not high enough for an equalization to occur.
    /// </summary>
    [ViewVariables]
    public int BreakdownCooldown = 0;
}
