using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract class SharedAtmosDebugOverlaySystem : EntitySystem
    {
        // Keep in mind, this system is hilariously unoptimized. The goal here is to provide accurate debug data.
        public const int LocalViewRange = 16;
        protected float AccumulatedFrameTime;

        [Serializable, NetSerializable]
        public readonly struct AtmosDebugOverlayData
        {
            public readonly float Temperature;
            public readonly float[] Moles;
            public readonly AtmosDirection PressureDirection;
            public readonly AtmosDirection LastPressureDirection;
            public readonly int InExcitedGroup;
            public readonly AtmosDirection BlockDirection;
            public readonly bool IsSpace;

            public AtmosDebugOverlayData(float temperature, float[] moles, AtmosDirection pressureDirection, AtmosDirection lastPressureDirection, int inExcited, AtmosDirection blockDirection, bool isSpace)
            {
                Temperature = temperature;
                Moles = moles;
                PressureDirection = pressureDirection;
                LastPressureDirection = lastPressureDirection;
                InExcitedGroup = inExcited;
                BlockDirection = blockDirection;
                IsSpace = isSpace;
            }
        }

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
            public AtmosDebugOverlayData[] OverlayData { get; }

            public AtmosDebugOverlayMessage(NetEntity gridIndices, Vector2i baseIdx, AtmosDebugOverlayData[] overlayData)
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
