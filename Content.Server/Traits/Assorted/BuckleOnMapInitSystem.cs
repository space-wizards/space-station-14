using Content.Shared.Buckle;

namespace Content.Server.Traits.Assorted;

public sealed class BuckleOnMapInitSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleOnMapInitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BuckleOnMapInitComponent component, MapInitEvent args)
    {
        var buckle = Spawn(component.Prototype, Transform(uid).Coordinates);
        _buckleSystem.TryBuckle(uid, uid, buckle);
    }
}
