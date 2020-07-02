using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleComponent : SharedBuckleComponent
    {
        private bool _buckled;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is BuckleComponentState buckle))
            {
                return;
            }

            _buckled = buckle.Buckled;
        }

        protected override bool Buckled => _buckled;
    }
}
