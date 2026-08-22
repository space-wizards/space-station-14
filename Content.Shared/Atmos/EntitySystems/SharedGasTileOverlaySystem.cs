using Content.Shared.Atmos.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedGasTileOverlaySystem : EntitySystem
{
    public const byte ChunkSize = 8;
    protected float AccumulatedFrameTime;
    protected bool PvsEnabled;

    [Dependency] protected IConfigurationManager ConfMan = default!;
    [Dependency] private SharedAtmosphereSystem _atmosphere = default!;

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
            if (gasPrototype.GasOverlaySprite != null)
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
        return new Vector2i((int)MathF.Floor((float)indices.X / ChunkSize), (int)MathF.Floor((float)indices.Y / ChunkSize));
    }

    [Serializable, NetSerializable]
    public readonly struct SharedFireData : IEquatable<SharedFireData>
    {
        [ViewVariables] public readonly byte FireState;
        // TODO change fire color based on ByteTemp

        public SharedFireData(byte fireState)
        {
            FireState = fireState;
        }

        public bool Equals(SharedFireData other)
        {
            return FireState == other.FireState;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct SharedVisibleGasData : IEquatable<SharedVisibleGasData>
    {
        [ViewVariables] public readonly byte[] Opacity;

        public SharedVisibleGasData(byte[] opacity)
        {
            Opacity = opacity;
        }

        public bool Equals(SharedVisibleGasData other)
        {
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
