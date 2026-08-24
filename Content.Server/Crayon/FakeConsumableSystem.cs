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
        if (!ent.Comp.DeleteOnThrow)
            return;

        var thrownCoords = Transform(ent).Coordinates;
        _popup.PopupCoordinates("The food vanishes in a mist...", thrownCoords);
        QueueDel(ent);
    }
}
