using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components
{
    [NetworkedComponent]
    public abstract class SharedSingularityComponent : Component
    {
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
