using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class CanUnarmedCombatCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            return context.GetState<SelfState>().GetValue()?.HasComponent<UnarmedCombatComponent>() ?? false ? 1.0f : 0.0f;
        }
    }
}
