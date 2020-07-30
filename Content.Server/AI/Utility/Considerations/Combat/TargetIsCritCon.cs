using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.DamageSystem;

namespace Content.Server.AI.Utility.Considerations.Combat
{
    public sealed class TargetIsCritCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !target.TryGetComponent(out IDamageableComponent damageableComponent))
            {
                return 0.0f;
            }

            if (damageableComponent.CurrentDamageState == DamageState.Critical)
            {
                return 1.0f;
            }

            return 0.0f;
        }
    }
}
