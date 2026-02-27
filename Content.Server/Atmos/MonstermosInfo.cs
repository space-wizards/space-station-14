using Content.Shared.Atmos;

namespace Content.Server.Atmos;

/// <summary>
/// Atmospherics class that stores data on tiles for Monstermos calculations and operations.
/// </summary>
public struct MonstermosInfo
{
    /// <summary>
    /// The last cycle this tile was processed for monstermos calculations.
    /// Used to determine if Monstermos has already processed this tile in the
    /// current tick's processing run.
    /// </summary>
    [ViewVariables]
    public int LastCycle;

    /// <summary>
    /// <para>The last global cycle (on the GridAtmosphereComponent) this tile was processed for
    /// monstermos calculations.
    /// Monstermos can process multiple groups, and these groups may intersect with each other.
    /// This allows Monstermos to check if a tile belongs to another group that has already been processed,
    /// and skip processing it again.</para>
    ///
    /// <para>Used for exploring the current area for determining tiles that should be equalized
    /// using a BFS fill (see https://en.wikipedia.org/wiki/Breadth-first_search)</para>
    /// </summary>
    [ViewVariables]
    public long LastQueueCycle;

    /// <summary>
    /// Similar to <see cref="LastQueueCycle"/>. Monstermos performs a second slow pass after the main
    /// BFS fill in order to build a gradient map to determine transfer directions and amounts.
    /// This field also tracks if we've already processed this tile in that slow pass so we don't re-queue it.
    /// </summary>
    [ViewVariables]
    public long LastSlowQueueCycle;

    /// <summary>
    /// Difference in the amount of moles in this tile compared to the tile's neighbors.
    /// Used to determine "how strongly" air wants to flow in/out of this tile from/to its neighbors.
    /// </summary>
    [ViewVariables]
    public float MoleDelta;

    /// <summary>
    /// Number of moles that are going to be transferred in this direction during final equalization.
    /// </summary>
    [ViewVariables]
    public float TransferDirectionEast;

    /// <summary>
    /// Number of moles that are going to be transferred in this direction during final equalization.
    /// </summary>
    [ViewVariables]
    public float TransferDirectionWest;

    /// <summary>
    /// Number of moles that are going to be transferred in this direction during final equalization.
    /// </summary>
    [ViewVariables]
    public float TransferDirectionNorth;

    /// <summary>
    /// Number of moles that are going to be transferred in this direction during final equalization.
    /// </summary>
    [ViewVariables]
    public float TransferDirectionSouth;

    /// <summary>
    /// <para>Number of moles that are going to be transferred to this tile during final equalization.
    /// You can think of this as molar flow rate, or the amount of air currently flowing through this tile.
    /// Used for space wind and airflow sounds during explosive decompression or big movements.</para>
    ///
    /// <para>During equalization calculations, Monstermos determines how much air is going to be transferred
    /// between tiles, and sums that up into this field. It then either
    ///
    /// determines how many moles to transfer in the direction of <see cref="CurrentTransferDirection"/>, or
    ///
    /// determines how many moles to move in each direction using <see cref="MoleDelta"/>,
    /// setting the TransferDirection fields accordingly based on the ratio obtained
    /// from <see cref="MoleDelta"/>.</para>
    /// </summary>
    [ViewVariables]
    public float CurrentTransferAmount;

    /// <summary>
    /// A pointer from the current tile to the direction in which air is being transferred the most.
    /// </summary>
    [ViewVariables]
    public AtmosDirection CurrentTransferDirection;

    /// <summary>
    /// Marks this tile as being equalized using the O(n log n) algorithm.
    /// </summary>
    [ViewVariables]
    public bool FastDone;

    /// <summary>
    /// Gets or sets the TransferDirection in the given direction.
    /// </summary>
    /// <param name="direction"></param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid direction is given
    /// (a non-cardinal direction)</exception>
    public float this[AtmosDirection direction]
    {
        get =>
            direction switch
            {
                AtmosDirection.East => TransferDirectionEast,
                AtmosDirection.West => TransferDirectionWest,
                AtmosDirection.North => TransferDirectionNorth,
                AtmosDirection.South => TransferDirectionSouth,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };

        set
        {
            switch (direction)
            {
                case AtmosDirection.East:
                    TransferDirectionEast = value;
                    break;
                case AtmosDirection.West:
                    TransferDirectionWest = value;
                    break;
                case AtmosDirection.North:
                    TransferDirectionNorth = value;
                    break;
                case AtmosDirection.South:
                    TransferDirectionSouth = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }

    /// <summary>
    /// Gets or sets the TransferDirection by index.
    /// </summary>
    /// <param name="index">The index of the direction</param>
    public float this[int index]
    {
        get => this[(AtmosDirection) (1 << index)];
        set => this[(AtmosDirection) (1 << index)] = value;
    }
}
