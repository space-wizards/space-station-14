using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem : EntitySystem
{
    public void InitializeActionDoAfter()
    {
        SubscribeLocalEvent<DoAfterComponent, ActionAttemptDoAfterEvent>(OnActionDoAfterAttempt);
    }

    private void OnActionDoAfterAttempt(Entity<DoAfterComponent> ent, ref ActionAttemptDoAfterEvent args)
    {
        // relay to user
        if (!TryComp<DoAfterComponent>(args.Performer, out var userDoAfter))
            return;

        // Check DoAfterArgs Settings
        if (!TryComp<DoAfterArgsComponent>(ent.Owner,  out var doAfterArgsComp))
            return;

        var delay = doAfterArgsComp.Delay;

        var actionDoAfterEvent = new ActionDoAfterEvent(args.Performer, args.OriginalUseDelay, args.Input);

        // TODO: Should add a raise on used in the attemptactiondoafterevent or something to add a conditional item or w/e

        var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, delay, actionDoAfterEvent, ent.Owner, args.Performer)
        {
            AttemptFrequency = doAfterArgsComp.AttemptFrequency,
            Broadcast = doAfterArgsComp.Broadcast,
            Hidden = doAfterArgsComp.Hidden,
            NeedHand = doAfterArgsComp.NeedHand,
            BreakOnHandChange = doAfterArgsComp.BreakOnHandChange,
            BreakOnDropItem = doAfterArgsComp.BreakOnDropItem,
            BreakOnMove = doAfterArgsComp.BreakOnMove,
            BreakOnWeightlessMove = doAfterArgsComp.BreakOnWeightlessMove,
            MovementThreshold = doAfterArgsComp.MovementThreshold,
            DistanceThreshold = doAfterArgsComp.DistanceThreshold,
            BreakOnDamage = doAfterArgsComp.BreakOnDamage,
            DamageThreshold = doAfterArgsComp.DamageThreshold,
            RequireCanInteract = doAfterArgsComp.RequireCanInteract
        };

        _doAfter.TryStartDoAfter(doAfterArgs, userDoAfter);
    }
}
