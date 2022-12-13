using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;

namespace Content.Client.Pinpointer;

public sealed class NavMapSystem : SharedNavMapSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<NavMapComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<NavMapComponent>(ev.EntityUid);
    }

    private void OnHandleState(EntityUid uid, NavMapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NavMapComponentState state)
            return;

        component.Chunks.Clear();

        foreach (var (origin, data) in state.TileData)
        {
            component.Chunks.Add(origin, new NavMapChunk(origin)
            {
                TileData = data,
            });
        }
    }
}
