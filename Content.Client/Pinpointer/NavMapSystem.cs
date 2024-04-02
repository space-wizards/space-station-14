using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Client.Pinpointer;

public sealed partial class NavMapSystem : SharedNavMapSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NavMapComponent, ComponentHandleState>(OnHandleState);

        SubscribeNetworkEvent<NavMapChunkChangedEvent>(OnChunkChanged);
        SubscribeNetworkEvent<NavMapBeaconChangedEvent>(OnBeaconChanged);
    }

    private void OnHandleState(EntityUid uid, NavMapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NavMapComponentState state)
            return;

        component.Chunks.Clear();

        foreach (var ((category, origin), chunk) in state.ChunkData)
        {
            var newChunk = new NavMapChunk(origin);

            foreach (var (atmosDirection, value) in chunk)
                newChunk.TileData[atmosDirection] = value;

            component.Chunks[(category, origin)] = newChunk;
        }

        component.Beacons.Clear();
        component.Beacons.AddRange(state.Beacons);
    }

    private void OnChunkChanged(NavMapChunkChangedEvent ev)
    {
        var gridUid = GetEntity(ev.Grid);

        if (!TryComp<NavMapComponent>(gridUid, out var component))
            return;

        var newChunk = new NavMapChunk(ev.ChunkOrigin);

        foreach (var (atmosDirection, value) in ev.TileData)
            newChunk.TileData[atmosDirection] = value;

        component.Chunks[(ev.Category, ev.ChunkOrigin)] = newChunk;
    }

    private void OnBeaconChanged(NavMapBeaconChangedEvent ev)
    {
        var gridUid = GetEntity(ev.Grid);

        if (!TryComp<NavMapComponent>(gridUid, out var component))
            return;

        var existing = component.Beacons.FirstOrNull(x => x.NetEnt == ev.Beacon.NetEnt);

        if (existing != null)
            component.Beacons.Remove(existing.Value);

        component.Beacons.Add(ev.Beacon);
    }
}
