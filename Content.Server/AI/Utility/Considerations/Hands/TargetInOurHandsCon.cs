using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;

namespace Content.Server.AI.Utility.Considerations.Hands
{
    /// <summary>
    /// Returns 1 if in our hands else 0
    /// </summary>
    public sealed class TargetInOurHandsCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null ||
                !target.HasComponent<ItemComponent>() ||
                !owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return 0.0f;
            }

            return handsComponent.IsHolding(target) ? 1.0f : 0.0f;
        }
    }
}
