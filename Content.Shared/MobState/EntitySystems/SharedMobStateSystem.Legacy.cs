using Content.Shared.Bed.Sleep;
using Content.Shared.Disease.Events;
using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Strip.Components;
using Content.Shared.Throwing;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{

    private void OnSleepAttempt(EntityUid uid, MobStateComponent component, ref TryingToSleepEvent args)
    {
        if(IsDead(uid, component))
            args.Cancelled = true;
    }

    private void OnSneezeAttempt(EntityUid uid, MobStateComponent component, ref AttemptSneezeCoughEvent args)
    {
        if(IsDead(uid, component))
            args.Cancelled = true;
    }

    private void OnGettingStripped(EntityUid uid, MobStateComponent component, BeforeGettingStrippedEvent args)
    {
        // Incapacitated or dead targets get stripped two or three times as fast. Makes stripping corpses less tedious.
        if (IsDead(uid, component))
            args.Multiplier /= 3;
        else if (IsCritical(uid, component))
            args.Multiplier /= 2;
    }

    private void OnStartPullAttempt(EntityUid uid, MobStateComponent component, StartPullAttemptEvent args)
    {
        if (IsIncapacitated(uid, component))
            args.Cancel();
    }

    #region ActionBlocker
        private void OnStateChanged(MobStateChangedEvent ev)
        {
            _blocker.UpdateCanMove(ev.Entity);
        }

        private void CheckAct(EntityUid uid, MobStateComponent component, CancellableEntityEventArgs args)
        {
            switch (component.CurrentState)
            {
                case MobState.Dead:
                case MobState.Critical:
                    args.Cancel();
                    break;
            }
        }

        private void OnChangeDirectionAttempt(EntityUid uid, MobStateComponent component, ChangeDirectionAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnUseAttempt(EntityUid uid, MobStateComponent component, UseAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnInteractAttempt(EntityUid uid, MobStateComponent component, InteractionAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnThrowAttempt(EntityUid uid, MobStateComponent component, ThrowAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnSpeakAttempt(EntityUid uid, MobStateComponent component, SpeakAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnEquipAttempt(EntityUid uid, MobStateComponent component, IsEquippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Equipee == uid)
                CheckAct(uid, component, args);
        }

        private void OnEmoteAttempt(EntityUid uid, MobStateComponent component, EmoteAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnUnequipAttempt(EntityUid uid, MobStateComponent component, IsUnequippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Unequipee == uid)
                CheckAct(uid, component, args);
        }

        private void OnDropAttempt(EntityUid uid, MobStateComponent component, DropAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnPickupAttempt(EntityUid uid, MobStateComponent component, PickupAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        #endregion

        private void OnMoveAttempt(EntityUid uid, MobStateComponent component, UpdateCanMoveEvent args)
        {
            switch (component.CurrentState)
            {
                case MobState.Critical:
                case MobState.Dead:
                    args.Cancel();
                    return;
                default:
                    return;
            }
        }

        private void OnStandAttempt(EntityUid uid, MobStateComponent component, StandAttemptEvent args)
        {
            if (IsIncapacitated(uid, component))
                args.Cancel();
        }

}
