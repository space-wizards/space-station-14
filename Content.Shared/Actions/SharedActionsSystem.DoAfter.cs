using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    protected void InitializeActionDoAfter()
    {
        SubscribeLocalEvent<DoAfterArgsComponent, ActionAttemptDoAfterEvent>(OnActionDoAfterAttempt);
    }

    private void OnActionDoAfterAttempt(Entity<DoAfterArgsComponent> ent, ref ActionAttemptDoAfterEvent args)
    {
        var performer = args.Performer;

        // relay to user
        if (!Resolve(performer, ref performer.Comp))
            return;

        var delay = ent.Comp.Delay;

        var netEnt = GetNetEntity(performer);

        var actionDoAfterEvent = new ActionDoAfterEvent(netEnt, args.OriginalUseDelay, args.Input);

        var doAfterArgs = new DoAfterArgs(EntityManager, performer, delay, actionDoAfterEvent, ent.Owner, args.Performer)
        {
            AttemptFrequency = ent.Comp.AttemptFrequency,
            Broadcast = ent.Comp.Broadcast,
            Hidden = ent.Comp.Hidden,
            NeedHand = ent.Comp.NeedHand,
            BreakOnHandChange = ent.Comp.BreakOnHandChange,
            BreakOnDropItem = ent.Comp.BreakOnDropItem,
            BreakOnMove = ent.Comp.BreakOnMove,
            BreakOnWeightlessMove = ent.Comp.BreakOnWeightlessMove,
            MovementThreshold = ent.Comp.MovementThreshold,
            DistanceThreshold = ent.Comp.DistanceThreshold,
            BreakOnDamage = ent.Comp.BreakOnDamage,
            DamageThreshold = ent.Comp.DamageThreshold,
            RequireCanInteract = ent.Comp.RequireCanInteract
        };

        _doAfter.TryStartDoAfter(doAfterArgs, performer);
    }
}
