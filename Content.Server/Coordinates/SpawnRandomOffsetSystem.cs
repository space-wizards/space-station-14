using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Server.Coordinates;

public sealed class SpawnRandomOffsetSystem : EntitySystem
{
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnRandomOffsetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SpawnRandomOffsetComponent component, MapInitEvent args)
    {
        _randomHelper.RandomOffset(uid, component.Offset);
        EntityManager.RemoveComponentDeferred(uid, component);
    }
}
