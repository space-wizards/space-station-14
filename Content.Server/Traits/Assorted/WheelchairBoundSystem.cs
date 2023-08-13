using Content.Shared.Buckle;
using Content.Shared.Traits.Assorted;

namespace Content.Server.Traits.Assorted;

public sealed class WheelchairBoundSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WheelchairBoundComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, WheelchairBoundComponent component, ComponentStartup args)
    {
        var wheelchair = Spawn(component.WheelchairPrototype, Transform(uid).Coordinates);
        _buckleSystem.TryBuckle(uid, uid, wheelchair);
    }
}
