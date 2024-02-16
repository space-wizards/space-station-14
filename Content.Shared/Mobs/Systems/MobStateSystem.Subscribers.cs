using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.ForceSay;
using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pointing;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Strip.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{
    //General purpose event subscriptions. If you can avoid it register these events inside their own systems
    private void SubscribeEvents()
    {
        SubscribeLocalEvent<MobStateComponent, BeforeGettingStrippedEvent>(OnGettingStripped);
        SubscribeLocalEvent<MobStateComponent, ChangeDirectionAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, UseAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, AttackAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, InteractionAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, ThrowAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<MobStateComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<MobStateComponent, EmoteAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<MobStateComponent, DropAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, PickupAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, StartPullAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, UpdateCanMoveEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, StandAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, PointAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MobStateComponent, TryingToSleepEvent>(OnSleepAttempt);
        SubscribeLocalEvent<MobStateComponent, CombatModeShouldHandInteractEvent>(OnCombatModeShouldHandInteract);
        SubscribeLocalEvent<MobStateComponent, AttemptPacifiedAttackEvent>(OnAttemptPacifiedAttack);
        SubscribeLocalEvent<MobStateComponent, OnHitScanHitEvent>(OnHitScanHit);
        SubscribeLocalEvent<MobStateComponent, TryRevertCollisionChangeEvent>(OnTryRevertCollisionChange);
    }

    private void OnStateExitSubscribers(EntityUid target, MobStateComponent component, MobState state)
    {
        switch (state)
        {
            case MobState.Alive:
                //unused
                break;
            case MobState.Critical:
                //unused
                break;
            case MobState.Dead:
                // Makes someone buckled able to be hit again while not aimed at.
                if(_buckle.IsBuckled(target))
                    _standing.RevertCollisionChange(target);
                break;
            case MobState.Invalid:
                //unused
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void OnStateEnteredSubscribers(EntityUid target, MobStateComponent component, MobState state)
    {
        // All of the state changes here should already be networked, so we do nothing if we are currently applying a
        // server state.
        if (_timing.ApplyingState)
            return;

        _blocker.UpdateCanMove(target); //update movement anytime a state changes
        switch (state)
        {
            case MobState.Alive:
                _standing.Stand(target);
                _appearance.SetData(target, MobStateVisuals.State, MobState.Alive);
                break;
            case MobState.Critical:
                _standing.Down(target);
                _appearance.SetData(target, MobStateVisuals.State, MobState.Critical);
                break;
            case MobState.Dead:
                _standing.Down(target);
                // Makes someone that dies while buckled unable to be hit unless aimed at.
                if(_buckle.IsBuckled(target))
                    _standing.ChangeCollision(target);
                _appearance.SetData(target, MobStateVisuals.State, MobState.Dead);
                break;
            case MobState.Invalid:
                //unused;
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private bool CheckDead(MobStateComponent component)
    {
        return component.CurrentState == MobState.Dead;
    }

    #region Event Subscribers

    private void OnSleepAttempt(EntityUid target, MobStateComponent component, ref TryingToSleepEvent args)
    {
        if (IsDead(target, component))
            args.Cancelled = true;
    }

    private void OnGettingStripped(EntityUid target, MobStateComponent component, BeforeGettingStrippedEvent args)
    {
        // Incapacitated or dead targets get stripped two or three times as fast. Makes stripping corpses less tedious.
        if (IsDead(target, component))
            args.Multiplier /= 3;
        else if (IsCritical(target, component))
            args.Multiplier /= 2;
    }

    private void OnSpeakAttempt(EntityUid uid, MobStateComponent component, SpeakAttemptEvent args)
    {
        if (HasComp<AllowNextCritSpeechComponent>(uid))
        {
            RemCompDeferred<AllowNextCritSpeechComponent>(uid);
            return;
        }

        CheckAct(uid, component, args);
    }

    private void CheckAct(EntityUid target, MobStateComponent component, CancellableEntityEventArgs args)
    {
        switch (component.CurrentState)
        {
            case MobState.Dead:
            case MobState.Critical:
                args.Cancel();
                break;
        }
    }

    private void OnEquipAttempt(EntityUid target, MobStateComponent component, IsEquippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Equipee == target)
            CheckAct(target, component, args);
    }

    private void OnUnequipAttempt(EntityUid target, MobStateComponent component, IsUnequippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Unequipee == target)
            CheckAct(target, component, args);
    }

    private void OnCombatModeShouldHandInteract(EntityUid uid, MobStateComponent component, ref CombatModeShouldHandInteractEvent args)
    {
        // Disallow empty-hand-interacting in combat mode
        // for non-dead mobs
        if (!IsDead(uid, component))
            args.Cancelled = true;
    }

    private void OnAttemptPacifiedAttack(Entity<MobStateComponent> ent, ref AttemptPacifiedAttackEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    ///     Checks if the hitscan is able to hit the target it's colliding with.
    /// </summary>
    private void OnHitScanHit(Entity<MobStateComponent> ent, ref OnHitScanHitEvent args)
    {
        //Always hit the target if it was aimed at by the player.
        if (args.GunTarget == args.HitEntity)
            return;

        //Always hit targets that are buckled and not dead.
        if(ent.Comp.CurrentState != MobState.Dead &&
           _buckle.IsBuckled(ent))
            return;

        //Always hit targets that are standing and not dead.
        if(ent.Comp.CurrentState != MobState.Dead &&
           !_standing.IsDown(ent))
            return;

        args.Cancelled = true;
    }

    private void OnTryRevertCollisionChange(Entity<MobStateComponent> ent, ref TryRevertCollisionChangeEvent args)
    {
        args.Cancelled = CheckDead(ent);
    }


    #endregion
}
