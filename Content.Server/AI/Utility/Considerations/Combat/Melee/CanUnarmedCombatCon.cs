using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class CanUnarmedCombatCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = context.GetState<SelfState>().GetValue();
            return entityManager.HasComponent<MeleeWeaponComponent>(entity) ? 1.0f : 0.0f;
        }
    }
}
