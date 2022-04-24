/*
using Content.Shared.Movement.Components;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;


namespace Content.Client.Nutrition.Components
{
    [RegisterComponent]
    public sealed class ThirstComponent : SharedThirstComponent
    {
        public override ThirstThreshold CurrentThirstThreshold { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not ThirstComponentState thirst)
            {
                return;
            }

            CurrentThirstThreshold = thirst.CurrentThreshold;
        }
    }
}
*/
