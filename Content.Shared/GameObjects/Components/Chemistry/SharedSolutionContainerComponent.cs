using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SharedSolutionContainerComponent : Component
    {
        public override string Name => "SolutionContainer";

        /// <inheritdoc />
        public sealed override uint? NetID => ContentNetIDs.SOLUTION;

        [Serializable, NetSerializable]
        public class SolutionComponentState : ComponentState
        {
            public SolutionComponentState() : base(ContentNetIDs.SOLUTION) { }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new SolutionComponentState();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState == null)
            {
                return;
            }
                
            // var compState = (SolutionComponentState)curState;
            // Is there anything we even need to sync with client?
        }
    }
}
