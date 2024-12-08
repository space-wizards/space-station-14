using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.NPC;
using Robust.Shared.Player;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    /*
     * Handles ActiveInputMoverComponent which is added and removed for query reasons.
     */

    private void InitializeActive()
    {
        SubscribeLocalEvent<InputMoverComponent, PlayerAttachedEvent>(OnInputPlayerAttached);
        SubscribeLocalEvent<InputMoverComponent, PlayerDetachedEvent>(OnInputPlayerDetached);
        SubscribeLocalEvent<InputMoverComponent, MobStateChangedEvent>(OnInputMobState);
    }

    protected virtual void OnInputPlayerAttached(Entity<InputMoverComponent> ent, ref PlayerAttachedEvent args)
    {
        SetMoveInput(ent, MoveButtons.None);
        RefreshActiveInput((ent.Owner, ent.Comp, null));
    }

    protected virtual void OnInputPlayerDetached(Entity<InputMoverComponent> ent, ref PlayerDetachedEvent args)
    {
        SetMoveInput(ent, MoveButtons.None);
        RefreshActiveInput((ent.Owner, ent.Comp, null));
    }

    private void OnInputMobState(Entity<InputMoverComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshActiveInput((ent.Owner, ent.Comp, args.Component));
    }

    public void RefreshActiveInput(Entity<InputMoverComponent?, MobStateComponent?> entity)
    {
        if (TerminatingOrDeleted(entity.Owner))
            return;

        if (!MoverQuery.Resolve(entity.Owner, ref entity.Comp1, false))
        {
            return;
        }

        if (Resolve(entity.Owner, ref entity.Comp2, false))
        {
            if (_mobState.IsIncapacitated(entity.Owner, entity.Comp2))
            {
                RemCompDeferred<ActiveInputMoverComponent>(entity.Owner);
                return;
            }
        }

        if (!HasComp<ActorComponent>(entity.Owner) &&
            !HasComp<ActiveNPCComponent>(entity.Owner))
        {
            RemCompDeferred<ActiveInputMoverComponent>(entity.Owner);
            return;
        }

        EnsureComp<ActiveInputMoverComponent>(entity.Owner);
    }
}
