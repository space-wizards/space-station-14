using Content.Shared.Explosion;
using Robust.Shared.Player;

namespace Content.Server.GhostKick;

public sealed class GhostKickUserOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostKickUserOnTriggerComponent, TriggerEvent>(HandleMineTriggered);
    }

    private void HandleMineTriggered(EntityUid uid, GhostKickUserOnTriggerComponent userOnTriggerComponent, ref TriggerEvent args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        _ghostKickManager.DoDisconnect(
            actor.PlayerSession.Channel,
            "Tripped over a kick mine, crashed through the fourth wall");

        args.Handled = true;
    }
}
