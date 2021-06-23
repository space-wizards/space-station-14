using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components
{

    public abstract class SharedSingularityComponent : Component
    {
        public override string Name => "Singularity";
        public override uint? NetID => ContentNetIDs.SINGULARITY;


        [Serializable, NetSerializable]
        protected sealed class SingularityComponentState : ComponentState
        {
            public int Level { get; }

            public SingularityComponentState(int level) : base(ContentNetIDs.SINGULARITY)
            {
                Level = level;
            }
        }
    }
}
