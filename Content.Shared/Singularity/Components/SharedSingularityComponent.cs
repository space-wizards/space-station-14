using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Singularity.Components
{
    [NetworkedComponent]
    public abstract class SharedSingularityComponent : Component
    {
        public override string Name => "Singularity";

        /// <summary>
        ///     The radiation pulse component's radsPerSecond is set to the singularity's level multiplied by this number.
        /// </summary>
        [DataField("radsPerLevel")]
        public float RadsPerLevel = 1;

        /// <summary>
        ///     Changed by <see cref="SharedSingularitySystem.ChangeSingularityLevel"/>
        /// </summary>
        [ViewVariables]
        public int Level { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SingularityComponentState state)
            {
                return;
            }

            EntitySystem.Get<SharedSingularitySystem>().ChangeSingularityLevel(this, state.Level);
        }
    }

    [Serializable, NetSerializable]
    public sealed class SingularityComponentState : ComponentState
    {
        public int Level { get; }

        public SingularityComponentState(int level)
        {
            Level = level;
        }
    }
}
