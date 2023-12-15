using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
public sealed class GasTileOverlayState : ComponentState, IComponentDeltaState
{
    public readonly Dictionary<Vector2i, GasOverlayChunk> Chunks;
    public bool FullState => AllChunks == null;

    // required to infer deleted/missing chunks for delta states
    public HashSet<Vector2i>? AllChunks;

    public GasTileOverlayState(Dictionary<Vector2i, GasOverlayChunk> chunks)
    {
        Chunks = chunks;
    }

    public void ApplyToFullState(ComponentState fullState)
    {
        DebugTools.Assert(!FullState);
        var state = (GasTileOverlayState) fullState;
        DebugTools.Assert(state.FullState);

        foreach (var key in state.Chunks.Keys)
        {
            if (!AllChunks!.Contains(key))
                state.Chunks.Remove(key);
        }

        foreach (var (chunk, data) in Chunks)
        {
            state.Chunks[chunk] = new(data);
        }
    }

    public ComponentState CreateNewFullState(ComponentState fullState)
    {
        DebugTools.Assert(!FullState);
        var state = (GasTileOverlayState) fullState;
        DebugTools.Assert(state.FullState);

        var chunks = new Dictionary<Vector2i, GasOverlayChunk>(state.Chunks.Count);

        foreach (var (chunk, data) in Chunks)
        {
            chunks[chunk] = new(data);
        }

        foreach (var (chunk, data) in state.Chunks)
        {
            if (AllChunks!.Contains(chunk))
                chunks.TryAdd(chunk, new(data));
        }

        return new GasTileOverlayState(chunks);
    }
}
