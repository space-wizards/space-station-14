using Content.Server.GhostKick;
using Content.Shared.EntityEffects;
using Robust.Shared.Player;

namespace Content.Server.EntityEffects.Effects.Smite;

public sealed partial class GhostKickEntityEffectSystem : EntityEffectSystem<ActorComponent, GhostKick>
{
    [Dependency] private GhostKickManager _ghostKick = default!;

    protected override void Effect(Entity<ActorComponent> entity, ref EntityEffectEvent<GhostKick> args)
    {
        _ghostKick.DoDisconnect(entity.Comp.PlayerSession.Channel, Loc.GetString(args.Effect.Reason));
    }
}

public sealed partial class GhostKick : EntityEffectBase<GhostKick>
{
    [DataField(required: true)]
    public LocId Reason;
}
