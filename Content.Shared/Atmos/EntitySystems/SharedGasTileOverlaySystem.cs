using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract class SharedGasTileOverlaySystem : EntitySystem
    {
        public const byte ChunkSize = 8;
        protected float AccumulatedFrameTime;

        [Dependency] protected readonly IPrototypeManager ProtoMan = default!;

        /// <summary>
        ///     array of the ids of all visible gases.
        /// </summary>
        public int[] VisibleGasId = default!;

        public override void Initialize()
        {
            base.Initialize();

            List<int> visibleGases = new();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasPrototype = ProtoMan.Index<GasPrototype>(i.ToString());
                if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture) || !string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayState))
                    visibleGases.Add(i);
            }

            VisibleGasId = visibleGases.ToArray();
        }

        public static Vector2i GetGasChunkIndices(Vector2i indices)
        {
            return new((int) MathF.Floor((float) indices.X / ChunkSize), (int) MathF.Floor((float) indices.Y / ChunkSize));
        }

        [Serializable, NetSerializable]
        public readonly struct GasOverlayData : IEquatable<GasOverlayData>
        {
            public readonly byte FireState;
            public readonly byte[] Opacity;

            // TODO change fire color based on temps
            // But also: dont dirty on a 0.01 kelvin change in temperatures.
            // Either have a temp tolerance, or map temperature -> byte levels

            public GasOverlayData(byte fireState, byte[] opacity)
            {
                FireState = fireState;
                Opacity = opacity;
            }

            public bool Equals(GasOverlayData other)
            {
                if (FireState != other.FireState)
                    return false;

                if (Opacity?.Length != other.Opacity?.Length)
                    return false;

                if (Opacity != null && other.Opacity != null)
                {
                    for (var i = 0; i < Opacity.Length; i++)
                    {
                        if (Opacity[i] != other.Opacity[i])
                            return false;
                    }
                }

                return true;
            }
        }

        [Serializable, NetSerializable]
        public sealed class GasOverlayUpdateEvent : EntityEventArgs
        {
            public Dictionary<EntityUid, List<GasOverlayChunk>> UpdatedChunks = new();
            public Dictionary<EntityUid, HashSet<Vector2i>> RemovedChunks = new();
        }
    }
}
