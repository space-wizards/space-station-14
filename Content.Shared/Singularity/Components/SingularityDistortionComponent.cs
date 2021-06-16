using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Singularity.Components
{
    [RegisterComponent]
    public class SingularityDistortionComponent : Component
    {
        public override string Name => "SingularityDistortion";
        public override uint? NetID => ContentNetIDs.SINGULARITY_DISTORTION;

        [DataField("intensity")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Intensity { get; set; } = 2;

        [DataField("falloff")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Falloff { get; set; } = 7;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SingularityDistortionComponentState(Intensity, Falloff);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SingularityDistortionComponentState state)
            {
                return;
            }

            Intensity = state.Intensity;
            Falloff = state.Falloff;
        }
    }

    [Serializable, NetSerializable]
    public class SingularityDistortionComponentState : ComponentState
    {
        public SingularityDistortionComponentState(float intensity, float falloff) : base(ContentNetIDs.TAG)
        {
            Intensity = intensity;
            Falloff = falloff;
        }

        public float Intensity { get; }
        public float Falloff { get; }
    }
}
