using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract class SharedGasTileOverlaySystem : EntitySystem
    {
        public const byte ChunkSize = 8;
        protected float AccumulatedFrameTime;
        protected bool PvsEnabled;

        [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
        [Dependency] private readonly SharedAtmosphereSystem _atmosphere = default!;

        /// <summary>
        ///     array of the ids of all visible gases.
        /// </summary>
        public int[] VisibleGasId = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasTileOverlayComponent, ComponentGetState>(OnGetState);

            List<int> visibleGases = new();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasPrototype = _atmosphere.GetGas(i);
                if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture) || !string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayState))
                    visibleGases.Add(i);
            }

            VisibleGasId = visibleGases.ToArray();
        }

        private void OnGetState(EntityUid uid, GasTileOverlayComponent component, ref ComponentGetState args)
        {
            if (PvsEnabled && !args.ReplayState)
                return;

            // Should this be a full component state or a delta-state?
            if (args.FromTick <= component.CreationTick || args.FromTick <= component.ForceTick)
            {
                args.State = new GasTileOverlayState(component.Chunks);
                return;
            }

            var data = new Dictionary<Vector2i, GasOverlayChunk>();
            foreach (var (index, chunk) in component.Chunks)
            {
                if (chunk.LastUpdate >= args.FromTick)
                    data[index] = chunk;
            }

            args.State = new GasTileOverlayDeltaState(data, new(component.Chunks.Keys));
        }

        public static Vector2i GetGasChunkIndices(Vector2i indices)
        {
            return new((int) MathF.Floor((float) indices.X / ChunkSize), (int) MathF.Floor((float) indices.Y / ChunkSize));
        }

        [Serializable, NetSerializable]
        public readonly struct GasOverlayData : IEquatable<GasOverlayData>
        {
            [ViewVariables]
            public readonly byte FireState;

            [ViewVariables]
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
            public Dictionary<NetEntity, List<GasOverlayChunk>> UpdatedChunks = new();
            public Dictionary<NetEntity, HashSet<Vector2i>> RemovedChunks = new();
        }
    }
}
