using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Mapping;

public abstract class RoofDebugOverlaySystem : EntitySystem
{
    public const int LocalViewRange = 16;
    protected float AccumulatedFrameTime;

    [Serializable, NetSerializable]
    public readonly record struct RoofDebugOverlayData(
        Vector2 Indices,
        bool HasRoof,
        bool HasEntity,
        bool Innate);

    /// <summary>
    ///     Invalid tiles for the roof overlay.
    ///     No point re-sending every tile if only a subset might have been updated.
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

