using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    /// <summary>
    ///     When attacked to an <see cref="IDamageableComponent"/>, this component will
    ///     handle critical and death behaviors for mobs.
    ///     Additionally, it handles sending effects to clients
    ///     (such as blur effect for unconsciousness) and managing the health HUD.
    /// </summary>
    public abstract class SharedMobStateManagerComponent : Component, IOnHealthChangedBehavior, IActionBlocker
    {
        public override string Name => "MobStateManager";

        public override uint? NetID => ContentNetIDs.MOB_STATE_MANAGER;

        protected abstract IReadOnlyDictionary<DamageState, IMobState> Behavior { get; }

        public virtual IMobState CurrentMobState { get; protected set; }

        public virtual DamageState CurrentDamageState { get; protected set; }

        public override void OnAdd()
        {
            base.OnAdd();

            CurrentDamageState = DamageState.Alive;
        }

        public override void Initialize()
        {
            base.Initialize();

            CurrentMobState = Behavior[CurrentDamageState];
            CurrentMobState.EnterState(Owner);
            CurrentMobState.UpdateState(Owner);
        }

        bool IActionBlocker.CanInteract()
        {
            return CurrentMobState.CanInteract();
        }

        bool IActionBlocker.CanMove()
        {
            return CurrentMobState.CanMove();
        }

        bool IActionBlocker.CanUse()
        {
            return CurrentMobState.CanUse();
        }

        bool IActionBlocker.CanThrow()
        {
            return CurrentMobState.CanThrow();
        }

        bool IActionBlocker.CanSpeak()
        {
            return CurrentMobState.CanSpeak();
        }

        bool IActionBlocker.CanDrop()
        {
            return CurrentMobState.CanDrop();
        }

        bool IActionBlocker.CanPickup()
        {
            return CurrentMobState.CanPickup();
        }

        bool IActionBlocker.CanEmote()
        {
            return CurrentMobState.CanEmote();
        }

        bool IActionBlocker.CanAttack()
        {
            return CurrentMobState.CanAttack();
        }

        bool IActionBlocker.CanEquip()
        {
            return CurrentMobState.CanEquip();
        }

        bool IActionBlocker.CanUnequip()
        {
            return CurrentMobState.CanUnequip();
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return CurrentMobState.CanChangeDirection();
        }

        public void OnHealthChanged(HealthChangedEventArgs e)
        {
            if (e.Damageable.CurrentDamageState != CurrentDamageState)
            {
                CurrentDamageState = e.Damageable.CurrentDamageState;
                CurrentMobState.ExitState(Owner);
                CurrentMobState = Behavior[CurrentDamageState];
                CurrentMobState.EnterState(Owner);
            }

            CurrentMobState.UpdateState(Owner);
        }
    }

    [Serializable, NetSerializable]
    public class MobStateManagerComponentState : ComponentState
    {
        public readonly DamageState DamageState;

        public MobStateManagerComponentState(DamageState damageState) : base(ContentNetIDs.MOB_STATE_MANAGER)
        {
            DamageState = damageState;
        }
    }
}
