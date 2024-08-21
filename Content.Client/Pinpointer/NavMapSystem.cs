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
        Dictionary<Vector2i, int[]> modifiedChunks;
        Dictionary<NetEntity, NavMapBeacon> beacons;

        switch (args.Current)
        {
            case NavMapDeltaState delta:
            {
                modifiedChunks = delta.ModifiedChunks;
                beacons = delta.Beacons;
                foreach (var index in component.Chunks.Keys)
                {
                    if (!delta.AllChunks!.Contains(index))
                        component.Chunks.Remove(index);
                }

                break;
            }
            case NavMapState state:
            {
                modifiedChunks = state.Chunks;
                beacons = state.Beacons;
                foreach (var index in component.Chunks.Keys)
                {
                    if (!state.Chunks.ContainsKey(index))
                        component.Chunks.Remove(index);
                }

                break;
            }
            default:
                return;
        }

        foreach (var (origin, chunk) in modifiedChunks)
        {
            var newChunk = new NavMapChunk(origin);
            Array.Copy(chunk, newChunk.TileData, chunk.Length);
            component.Chunks[origin] = newChunk;
        }

        component.Beacons.Clear();
        foreach (var (nuid, beacon) in beacons)
        {
            component.Beacons[nuid] = beacon;
        }
    }
}
