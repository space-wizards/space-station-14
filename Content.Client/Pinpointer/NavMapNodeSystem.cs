using Content.Shared.Pinpointer;

namespace Content.Client.Pinpointer;

public sealed class NavMapNodeSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NavMapAddNodesMessage>(OnNavMapAddNodes);
        SubscribeNetworkEvent<NavMapRemoveNodesMessage>(OnNavMapRemoveNodes);
    }

    private void OnNavMapAddNodes(NavMapAddNodesMessage ev)
    {
        if (!TryComp<NavMapNodeComponent>(GetEntity(ev.GridUid), out var comp))
            return;

        foreach (var hvNode in ev.HVNodes)
            comp.GridHVNodeCoords.Add(GetCoordinates(hvNode));

        foreach (var mvNode in ev.MVNodes)
            comp.GridMVNodeCoords.Add(GetCoordinates(mvNode));

        foreach (var lvNode in ev.LVNodes)
            comp.GridLVNodeCoords.Add(GetCoordinates(lvNode));
    }

    private void OnNavMapRemoveNodes(NavMapRemoveNodesMessage ev)
    {

    }
}
