using Content.Shared.Random.Helpers;

namespace Content.Server.Coordinates;

public sealed class SpawnRandomOffsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnRandomOffsetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SpawnRandomOffsetComponent component, MapInitEvent args)
    {
        // TODO: Kill this extension with fire, thanks
        uid.RandomOffset(component.Offset);
        EntityManager.RemoveComponentDeferred(uid, component);
    }
}
