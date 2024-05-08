using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;

namespace Content.Client.Pinpointer;

public sealed partial class NavMapSystem : SharedNavMapSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NavMapComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, NavMapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NavMapComponentState state)
            return;

        if (!state.FullState)
        {
            foreach (var index in component.Chunks.Keys)
            {
                if (!state.AllChunks!.Contains(index))
                    component.Chunks.Remove(index);
            }
        }
        else
        {
            foreach (var index in component.Chunks.Keys)
            {
                if (!state.Chunks.ContainsKey(index))
                    component.Chunks.Remove(index);
            }
        }

        foreach (var (origin, chunk) in state.Chunks)
        {
            var newChunk = new NavMapChunk(origin);
            Array.Copy(chunk, newChunk.TileData, chunk.Length);
            component.Chunks[origin] = newChunk;
        }

        component.Beacons.Clear();
        foreach (var (nuid, beacon) in state.Beacons)
        {
            component.Beacons[nuid] = beacon;
        }
    }
}
