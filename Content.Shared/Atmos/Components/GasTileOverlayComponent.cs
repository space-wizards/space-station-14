using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GasTileOverlayComponent : Component
{
    /// <summary>
    ///     The tiles that have had their atmos data updated since last tick
    /// </summary>
    public readonly HashSet<Vector2i> InvalidTiles = new();

    /// <summary>
    ///     Gas data stored in chunks to make PVS / bubbling easier.
    /// </summary>
    public readonly Dictionary<Vector2i, GasOverlayChunk> Chunks = new();

    /// <summary>
    ///     Tick at which PVS was last toggled. Ensures that all players receive a full update when toggling PVS.
    /// </summary>
    public GameTick ForceTick { get; set; }
}

[Serializable, NetSerializable]
public sealed class GasTileOverlayState(Dictionary<Vector2i, GasOverlayChunk> chunks) : ComponentState
{
    public readonly Dictionary<Vector2i, GasOverlayChunk> Chunks = chunks;
}

[Serializable, NetSerializable]
public sealed class GasTileOverlayDeltaState(
    Dictionary<Vector2i, GasOverlayChunk> modifiedChunks,
    HashSet<Vector2i> allChunks)
    : ComponentState, IComponentDeltaState<GasTileOverlayState>
{
    public readonly Dictionary<Vector2i, GasOverlayChunk> ModifiedChunks = modifiedChunks;
    public readonly HashSet<Vector2i> AllChunks = allChunks;

    public void ApplyToFullState(GasTileOverlayState state)
    {
        foreach (var key in state.Chunks.Keys)
        {
            if (!AllChunks.Contains(key))
                state.Chunks.Remove(key);
        }

        foreach (var (chunk, data) in ModifiedChunks)
        {
            state.Chunks[chunk] = new(data);
        }
    }

    public GasTileOverlayState CreateNewFullState(GasTileOverlayState state)
    {
        var chunks = new Dictionary<Vector2i, GasOverlayChunk>(AllChunks.Count);

        foreach (var (chunk, data) in ModifiedChunks)
        {
            chunks[chunk] = new(data);
        }

        foreach (var (chunk, data) in state.Chunks)
        {
            if (AllChunks.Contains(chunk))
                chunks.TryAdd(chunk, new(data));
        }

        return new GasTileOverlayState(chunks);
    }
}
