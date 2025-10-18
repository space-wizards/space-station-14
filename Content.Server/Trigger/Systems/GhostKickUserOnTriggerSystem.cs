using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Content.Server.GhostKick;
using Robust.Shared.Player;

namespace Content.Server.Trigger.Systems;

public sealed class GhostKickUserOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostKickOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<GhostKickOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp(target, out ActorComponent? actor))
            return;

        _ghostKickManager.DoDisconnect(
            actor.PlayerSession.Channel,
            Loc.GetString(ent.Comp.Reason));

        args.Handled = true;
    }
}
