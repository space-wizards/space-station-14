using Content.Shared.Random.Helpers;

namespace Content.Server.Coordinates;

public sealed class SpawnRandomOffsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnRandomOffsetComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SpawnRandomOffsetComponent component, ComponentInit args)
    {
        // TODO: Kill this extension with fire, thanks
        uid.RandomOffset(component.Offset);
    }
}
