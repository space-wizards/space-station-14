using Content.Shared.Standing;
using Robust.Shared.GameObjects;


namespace Content.Shared.MobState.State
{
    /// <summary>
    ///     The standard state an entity is in; no negative effects.
    /// </summary>
    public abstract class SharedNormalMobState : BaseMobState
    {
        protected override DamageState DamageState => DamageState.Alive;

        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);
            EntitySystem.Get<StandingStateSystem>().Stand(entity);

            if (entity.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Alive);
            }
        }

        public override bool CanDrop()
        {
            return true;
        }

        public override bool CanPickup()
        {
            return true;
        }

        public override bool CanEmote()
        {
            return true;
        }

        public override bool CanAttack()
        {
            return true;
        }
    }
}
