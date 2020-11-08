#nullable enable
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
    public abstract class SharedMobStateComponent : Component, IActionBlocker
    {
        public override string Name => "MobState";

        public override uint? NetID => ContentNetIDs.MOB_STATE_MANAGER;

        private DamageState _damageState;

        protected abstract IReadOnlyDictionary<DamageState, IMobState> Behavior { get; }

        public virtual IMobState MobState { get; protected set; } = default!;

        public DamageState DamageState
        {
            get => _damageState;
            set
            {
                if (_damageState == value)
                {
                    return;
                }

                OnChangeState(_damageState, value);

                Dirty();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _damageState, "state", DamageState.Alive);
        }

        protected override void Startup()
        {
            base.Startup();

            OnChangeState(DamageState.Invalid, _damageState);
        }

        public override ComponentState GetComponentState()
        {
            return new MobStateComponentState(DamageState);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is MobStateComponentState state))
            {
                return;
            }

            _damageState = state.DamageState;
            MobState.ExitState(Owner);
            MobState = Behavior[DamageState];
            MobState.EnterState(Owner);
        }

        private void OnChangeState(DamageState old, DamageState current)
        {
            if (old != DamageState.Invalid)
            {
                MobState.ExitState(Owner);
            }

            _damageState = current;
            MobState = Behavior[DamageState];
            MobState.EnterState(Owner);
            MobState.UpdateState(Owner);
        }

        bool IActionBlocker.CanInteract()
        {
            return MobState.CanInteract();
        }

        bool IActionBlocker.CanMove()
        {
            return MobState.CanMove();
        }

        bool IActionBlocker.CanUse()
        {
            return MobState.CanUse();
        }

        bool IActionBlocker.CanThrow()
        {
            return MobState.CanThrow();
        }

        bool IActionBlocker.CanSpeak()
        {
            return MobState.CanSpeak();
        }

        bool IActionBlocker.CanDrop()
        {
            return MobState.CanDrop();
        }

        bool IActionBlocker.CanPickup()
        {
            return MobState.CanPickup();
        }

        bool IActionBlocker.CanEmote()
        {
            return MobState.CanEmote();
        }

        bool IActionBlocker.CanAttack()
        {
            return MobState.CanAttack();
        }

        bool IActionBlocker.CanEquip()
        {
            return MobState.CanEquip();
        }

        bool IActionBlocker.CanUnequip()
        {
            return MobState.CanUnequip();
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return MobState.CanChangeDirection();
        }
    }

    [Serializable, NetSerializable]
    public class MobStateComponentState : ComponentState
    {
        public readonly DamageState DamageState;

        public MobStateComponentState(DamageState damageState) : base(ContentNetIDs.MOB_STATE_MANAGER)
        {
            DamageState = damageState;
        }
    }
}
