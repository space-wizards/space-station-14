using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Content.Shared.Movement;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.EntitySystems
{
    public sealed class MobStateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MobStateComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
            SubscribeLocalEvent<MobStateComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<MobStateComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<MobStateComponent, ThrowAttemptEvent>(OnThrowAttempt);
            SubscribeLocalEvent<MobStateComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MobStateComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<MobStateComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<MobStateComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<MobStateComponent, AttackAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<MobStateComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<MobStateComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<MobStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
            SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(UpdateState);
            SubscribeLocalEvent<MobStateComponent, MovementAttemptEvent>(OnMoveAttempt);
            SubscribeLocalEvent<MobStateComponent, StandAttemptEvent>(OnStandAttempt);
            // Note that there's no check for Down attempts because if a mob's in crit or dead, they can be downed...
        }

        #region ActionBlocker

        private void CheckAct(EntityUid uid, MobStateComponent component, CancellableEntityEventArgs args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
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

        private void OnAttackAttempt(EntityUid uid, MobStateComponent component, AttackAttemptEvent args)
        {
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

        private void OnStartPullAttempt(EntityUid uid, MobStateComponent component, StartPullAttemptEvent args)
        {
            if (component.IsIncapacitated())
                args.Cancel();
        }

        public void UpdateState(EntityUid _, MobStateComponent component, DamageChangedEvent args)
        {
            component.UpdateState(args.Damageable.TotalDamage);
        }

        private void OnMoveAttempt(EntityUid uid, MobStateComponent component, MovementAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedCriticalMobState:
                case SharedDeadMobState:
                    args.Cancel();
                    return;
                default:
                    return;
            }
        }

        private void OnStandAttempt(EntityUid uid, MobStateComponent component, StandAttemptEvent args)
        {
            if (component.IsIncapacitated())
                args.Cancel();
        }
    }
}
