using Content.Server.Popups;
using Content.Shared.Crayon;
using Content.Shared.Throwing;

namespace Content.Server.Crayon;

public sealed partial class FakeConsumableSystem : EntitySystem
{
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FakeConsumableComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnStopThrow(Entity<FakeConsumableComponent> ent, ref StopThrowEvent args)
    {
        var thrownCoords = Transform(ent).Coordinates;
        _popup.PopupCoordinates(Loc.GetString("fake-food-component-vanish", ("owner", ent)), thrownCoords);
        QueueDel(ent);
    }
}
