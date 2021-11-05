using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.MobState.State
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class BaseMobState : IMobState
    {
        protected abstract DamageState DamageState { get; }

        public virtual bool IsAlive()
        {
            return DamageState == DamageState.Alive;
        }

        public virtual bool IsCritical()
        {
            return DamageState == DamageState.Critical;
        }

        public virtual bool IsDead()
        {
            return DamageState == DamageState.Dead;
        }

        public virtual bool IsIncapacitated()
        {
            return IsCritical() || IsDead();
        }

        public virtual void EnterState(IEntity entity) { }

        public virtual void ExitState(IEntity entity) { }

        public virtual void UpdateState(IEntity entity, FixedPoint2 threshold) { }
    }
}
