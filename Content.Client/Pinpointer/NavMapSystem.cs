using System.Linq;
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
        Dictionary<NetEntity, NavMapRegionProperties> regions;

        switch (args.Current)
        {
            case NavMapDeltaState delta:
            {
                modifiedChunks = delta.ModifiedChunks;
                beacons = delta.Beacons;
                regions = delta.Regions;

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
                regions = state.Regions;

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

            // If the affected chunk intersects one or more regions, re-flood them
            if (!_chunkToRegionOwnerTable.TryGetValue(origin, out var affectedOwners))
                continue;

            foreach (var affectedOwner in affectedOwners)
            {
                if (!component.RegionProperties.ContainsKey(affectedOwner))
                    continue;

                if (!component.QueuedRegionsToFlood.Contains(affectedOwner))
                    component.QueuedRegionsToFlood.Enqueue(affectedOwner);
            }
        }

        component.Beacons.Clear();
        foreach (var (nuid, beacon) in beacons)
        {
            component.Beacons[nuid] = beacon;
        }

        foreach (var (nuid, region) in regions)
        {
            component.RegionProperties[nuid] = region;

            if (!component.QueuedRegionsToFlood.Contains(nuid))
                component.QueuedRegionsToFlood.Enqueue(nuid);
        }
    }
}
