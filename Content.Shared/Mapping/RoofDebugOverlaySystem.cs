using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Mapping;

public abstract class RoofDebugOverlaySystem : EntitySystem
{
    public const int LocalViewRange = 16;
    protected float AccumulatedFrameTime;

    /// <summary>
    /// Contains data for one tile for the overlay.
    /// </summary>
    /// <param name="Indices">The tile's indices on its grid</param>
    /// <param name="HasRoof">If the tile has a roof.</param>
    /// <param name="Innate">If the tile's grid has an implicit roof</param>
    [Serializable, NetSerializable]
    public readonly record struct RoofDebugOverlayData(
        Vector2 Indices,
        bool HasRoof,
        bool Innate);

    /// <summary>
    ///     Updates the client's roof overlay data for the provided grid.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RoofDebugOverlayMessage : EntityEventArgs
    {
        public NetEntity GridId { get; }

        public Vector2i BaseIdx { get; }
        // LocalViewRange*LocalViewRange
        public RoofDebugOverlayData?[] OverlayData { get; }

        public RoofDebugOverlayMessage(NetEntity gridIndices, Vector2i baseIdx, RoofDebugOverlayData?[] overlayData)
        {
            GridId = gridIndices;
            BaseIdx = baseIdx;
            OverlayData = overlayData;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RoofDebugOverlayDisableMessage : EntityEventArgs
    {
    }
}

