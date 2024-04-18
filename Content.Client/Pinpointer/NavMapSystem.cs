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

            foreach (var beacon in component.Beacons)
            {
                if (!state.AllBeacons!.Contains(beacon))
                    component.Beacons.Remove(beacon);
            }
        }

        else
        {
            foreach (var index in component.Chunks.Keys)
            {
                if (!state.Chunks.ContainsKey(index))
                    component.Chunks.Remove(index);
            }

            foreach (var beacon in component.Beacons)
            {
                if (!state.Beacons.Contains(beacon))
                    component.Beacons.Remove(beacon);
            }
        }

        foreach (var ((category, origin), chunk) in state.Chunks)
        {
            var newChunk = new NavMapChunk(origin);

            foreach (var (atmosDirection, value) in chunk)
                newChunk.TileData[atmosDirection] = value;

            component.Chunks[(category, origin)] = newChunk;
        }

        foreach (var beacon in state.Beacons)
            component.Beacons.Add(beacon);
    }
}
