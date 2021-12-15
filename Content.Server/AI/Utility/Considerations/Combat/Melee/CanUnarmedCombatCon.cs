using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class CanUnarmedCombatCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = context.GetState<SelfState>().GetValue();
            return entityManager.HasComponent<UnarmedCombatComponent>(entity) ? 1.0f : 0.0f;
        }
    }
}
