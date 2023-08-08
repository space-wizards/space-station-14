using Content.Shared.Buckle;

namespace Content.Server.Traits.Assorted;

public sealed class WheelchairBoundSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WheelchairBoundComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, WheelchairBoundComponent component, MapInitEvent args)
    {
        var wheelchair = Spawn(component.WheelchairPrototype, Transform(uid).Coordinates);
        _buckleSystem.TryBuckle(uid, uid, wheelchair);
    }
}
