using Robust.Server.GameObjects;

namespace Content.Server.LandMines;

public sealed class MineEffectKickSystem : EntitySystem
{
    [Dependency] private readonly KickMineManager _kickMineManager = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<MineEffectKickComponent, MineTriggeredEvent>(HandleMineTriggered);
    }

    private void HandleMineTriggered(EntityUid uid, MineEffectKickComponent component, MineTriggeredEvent args)
    {
        if (!TryComp(args.Tripper, out ActorComponent? actor))
            return;

        _kickMineManager.DoDisconnect(actor.PlayerSession.ConnectedClient);
    }
}
