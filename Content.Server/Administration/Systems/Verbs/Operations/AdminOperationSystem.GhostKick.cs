using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnGhostKick(Entity<ActorComponent> entity, ref AdminOperationEvent<GhostKickOperation> args)
    {
        _ghostKick.DoDisconnect(entity.Comp.PlayerSession.Channel, Loc.GetString(args.Operation.Reason));
    }
}
