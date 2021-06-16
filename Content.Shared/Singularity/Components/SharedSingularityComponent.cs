using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Singularity.Components
{
    public abstract class SharedSingularityComponent : Component
    {
        public override string Name => "Singularity";
        public override uint? NetID => ContentNetIDs.SINGULARITY;

        /// <summary>
        ///     Changed by <see cref="SingularitySystem.ChangeSingularityLevel"/>
        /// </summary>
        [ViewVariables]
        public int Level { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SingularityComponentState state)
            {
                return;
            }

            EntitySystem.Get<SingularitySystem>().ChangeSingularityLevel(this, state.Level);
        }
    }

    [Serializable, NetSerializable]
    public sealed class SingularityComponentState : ComponentState
    {
        public int Level { get; }

        public SingularityComponentState(int level) : base(ContentNetIDs.SINGULARITY)
        {
            Level = level;
        }
    }
}
