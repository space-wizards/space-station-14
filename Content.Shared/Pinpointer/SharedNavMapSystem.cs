using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

public abstract class SharedNavMapSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<NavMapComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, NavMapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NavMapComponentState state)
            return;

        component.Chunks.Clear();

        foreach (var (origin, data) in component.Chunks)
        {
            
        }
    }

    private void OnGetState(EntityUid uid, NavMapComponent component, ref ComponentGetState args)
    {
        var data = new Dictionary<Vector2i, List<Vector2[]>>(component.Chunks.Count);

        foreach (var (index, chunk) in component.Chunks)
        {
            var tileData = new List<Vector2[]>(chunk.TileData.Count);

            foreach (var tile in chunk.TileData.Values)
            {
                tileData.Add(tile);
            }

            data.Add(index, tileData);
        }

        // TODO: Dear lord this will need diffs.
        args.State = new NavMapComponentState()
        {
            TileData = data,
        };
    }

    [Serializable, NetSerializable]
    protected sealed class NavMapComponentState : ComponentState
    {
        public Dictionary<Vector2i, List<Vector2[]>> TileData = new();
    }
}
