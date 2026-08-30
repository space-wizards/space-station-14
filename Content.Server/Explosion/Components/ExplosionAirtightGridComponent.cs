using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Stores data for airtight explosion traversal on a <see cref="MapGridComponent"/> entity.
/// </summary>
/// <seealso cref="ExplosionSystem"/>
[RegisterComponent]
[Access(typeof(ExplosionSystem), Other = AccessPermissions.None)]
public sealed partial class ExplosionAirtightGridComponent : Component
{
    /// <summary>
    /// Data for every tile on the current grid.
    /// </summary>
    /// <remarks>
    /// Intentionally not saved.
    /// </remarks>
    [ViewVariables]
    public readonly Dictionary<Vector2i, TileData> Tiles = new();

    /// <summary>
    ///     Data struct that describes the explosion-blocking airtight entities on a tile.
    /// </summary>
    public struct TileData
    {
        /// <summary>
        /// Which index into the tolerance cache of <see cref="ExplosionSystem"/> this tile is using.
        /// </summary>
        public required int ToleranceCacheIndex;

        /// <summary>
        /// Which directions this tile is blocking explosions in. Bitflag field.
        /// </summary>
        public required AtmosDirection BlockedDirections;
    }

    /// <summary>
    /// A set of tolerance values
    /// </summary>
    public struct ToleranceValues : IEquatable<ToleranceValues>
    {
        /// <summary>
        /// Special value that indicates the entity is "invulnerable" against a specific explosion type.
        /// </summary>
        /// <remarks>
        /// Here to deal with the limited range of <see cref="FixedPoint2"/> over typical floats.
        /// </remarks>
        public static readonly FixedPoint2 Invulnerable = FixedPoint2.MaxValue;

        /// <summary>
        /// The intensities at which explosions of each type can instantly break through an entity.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is an array, with the index of each value corresponding to the "explosion type ID" cached by
        /// <see cref="ExplosionSystem"/>.
        /// </para>
        /// <para>
        /// Values are stored as <see cref="FixedPoint2"/> to avoid possible precision issues resulting in
        /// different-but-almost-identical tolerance values wasting memory.
        /// </para>
        /// <para>
        /// If a value is <see cref="Invulnerable"/>, that indicates the tile is invulnerable.
        /// </para>
        /// </remarks>
        public required FixedPoint2[] Values;

        public bool Equals(ToleranceValues other)
        {
            return Values.AsSpan().SequenceEqual(other.Values);
        }

        public override bool Equals(object? obj)
        {
            return obj is ToleranceValues other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.AddArray(Values);
            return hc.ToHashCode();
        }

        public static bool operator ==(ToleranceValues left, ToleranceValues right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ToleranceValues left, ToleranceValues right)
        {
            return !left.Equals(right);
        }
    }
}
