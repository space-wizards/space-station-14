using Robust.Shared.Physics.Dynamics;

namespace Content.Server.LandMines;

public sealed class LandMineSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LandMineComponent, StartCollideEvent>(HandleCollide);

    }

    private void HandleCollide(EntityUid uid, LandMineComponent component, StartCollideEvent args)
    {
        var ent = args.OtherFixture.Body.Owner;

        RaiseLocalEvent(uid, new MineTriggeredEvent { Tripper = ent });

        QueueDel(uid);
    }
}

public sealed class MineTriggeredEvent : EntityEventArgs
{
    public EntityUid Tripper;
}
