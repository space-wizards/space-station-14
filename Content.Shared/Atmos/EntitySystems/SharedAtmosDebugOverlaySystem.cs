using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract class SharedAtmosDebugOverlaySystem : EntitySystem
    {
        // Keep in mind, this system is hilariously unoptimized. The goal here is to provide accurate debug data.
        public const int LocalViewRange = 16;
        protected float AccumulatedFrameTime;

        [Serializable, NetSerializable]
        public readonly record struct AtmosDebugOverlayData(
            Vector2 Indices,
            float Temperature,
            float[]? Moles,
            AtmosDirection PressureDirection,
            AtmosDirection LastPressureDirection,
            AtmosDirection BlockDirection,
            int? InExcitedGroup,
            bool IsSpace,
            bool MapAtmosphere,
            bool NoGrid,
            bool Immutable);

        /// <summary>
        ///     Invalid tiles for the gas overlay.
        ///     No point re-sending every tile if only a subset might have been updated.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class AtmosDebugOverlayMessage : EntityEventArgs
        {
            public NetEntity GridId { get; }

            public Vector2i BaseIdx { get; }
            // LocalViewRange*LocalViewRange
            public AtmosDebugOverlayData?[] OverlayData { get; }

            public AtmosDebugOverlayMessage(NetEntity gridIndices, Vector2i baseIdx, AtmosDebugOverlayData?[] overlayData)
            {
                GridId = gridIndices;
                BaseIdx = baseIdx;
                OverlayData = overlayData;
            }
        }

        [Serializable, NetSerializable]
        public sealed class AtmosDebugOverlayDisableMessage : EntityEventArgs
        {
        }
    }
}
