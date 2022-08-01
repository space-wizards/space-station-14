using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.NPC.Utility.Considerations.Combat.Melee
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
