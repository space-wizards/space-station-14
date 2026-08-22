using Robust.Shared.Player;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnGhostKick(Entity<ActorComponent> entity, ref AdminOperationEvent<GhostKickOperation> args)
    {
        _ghostKick.DoDisconnect(entity.Comp.PlayerSession.Channel, Loc.GetString(args.Operation.Reason));
    }
}

public sealed partial class GhostKickOperation : AdminOperationBase<GhostKickOperation>
{
    [DataField(required: true)]
    public LocId Reason { get; private set; }
}
