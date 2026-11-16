using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Maps;

[RegisterComponent, NetworkedComponent]
public sealed partial class TileHistoryComponent : Component
{
    // History of tiles for each grid chunk.
    [DataField]
    public Dictionary<Vector2i, TileHistoryChunk> ChunkHistory = new();

    /// <summary>
    ///     Tick at which PVS was last toggled. Ensures that all players receive a full update when toggling PVS.
    /// </summary>
    public GameTick ForceTick { get; set; }
}

[Serializable, NetSerializable]
public sealed class TileHistoryState : ComponentState
{
    public Dictionary<Vector2i, TileHistoryChunk> ChunkHistory;

    public TileHistoryState(Dictionary<Vector2i, TileHistoryChunk> chunkHistory)
    {
        ChunkHistory = chunkHistory;
    }
}

[Serializable, NetSerializable]
public sealed class TileHistoryDeltaState : ComponentState, IComponentDeltaState<TileHistoryState>
{
    public Dictionary<Vector2i, TileHistoryChunk> ChunkHistory;
    public HashSet<Vector2i> AllHistoryChunks;

    public TileHistoryDeltaState(Dictionary<Vector2i, TileHistoryChunk> chunkHistory, HashSet<Vector2i> allHistoryChunks)
    {
        ChunkHistory = chunkHistory;
        AllHistoryChunks = allHistoryChunks;
    }

    public void ApplyToFullState(TileHistoryState state)
    {
        var toRemove = new List<Vector2i>();
        foreach (var key in state.ChunkHistory.Keys)
        {
            if (!AllHistoryChunks.Contains(key))
                toRemove.Add(key);
        }

        foreach (var key in toRemove)
        {
            state.ChunkHistory.Remove(key);
        }

        foreach (var (indices, chunk) in ChunkHistory)
        {
            state.ChunkHistory[indices] = new TileHistoryChunk(chunk);
        }
    }

    public void ApplyToComponent(TileHistoryComponent component)
    {
        var toRemove = new List<Vector2i>();
        foreach (var key in component.ChunkHistory.Keys)
        {
            if (!AllHistoryChunks.Contains(key))
                toRemove.Add(key);
        }

        foreach (var key in toRemove)
        {
            component.ChunkHistory.Remove(key);
        }

        foreach (var (indices, chunk) in ChunkHistory)
        {
            component.ChunkHistory[indices] = new TileHistoryChunk(chunk);
        }
    }

    public TileHistoryState CreateNewFullState(TileHistoryState state)
    {
        var chunks = new Dictionary<Vector2i, TileHistoryChunk>(state.ChunkHistory.Count);

        foreach (var (indices, chunk) in ChunkHistory)
        {
            chunks[indices] = new TileHistoryChunk(chunk);
        }

        foreach (var (indices, chunk) in state.ChunkHistory)
        {
            if (AllHistoryChunks.Contains(indices))
                chunks.TryAdd(indices, new TileHistoryChunk(chunk));
        }

        return new TileHistoryState(chunks);
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class TileHistoryChunk
{
    [DataField]
    public Dictionary<Vector2i, List<ProtoId<ContentTileDefinition>>> History = new();

    [ViewVariables]
    public GameTick LastModified;

    public TileHistoryChunk()
    {
    }

    public TileHistoryChunk(TileHistoryChunk other)
    {
        History = new Dictionary<Vector2i, List<ProtoId<ContentTileDefinition>>>(other.History.Count);
        foreach (var (key, value) in other.History)
        {
            History[key] = new List<ProtoId<ContentTileDefinition>>(value);
        }
        LastModified = other.LastModified;
    }
}
