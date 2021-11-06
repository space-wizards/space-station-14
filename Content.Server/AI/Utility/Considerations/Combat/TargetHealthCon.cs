using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Shared.Damage;

namespace Content.Server.AI.Utility.Considerations.Combat
{
    public sealed class TargetHealthCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || target.Deleted || !target.TryGetComponent(out DamageableComponent? damageableComponent))
            {
                return 0.0f;
            }

            return (float) damageableComponent.TotalDamage / 300.0f;
        }
    }
}
