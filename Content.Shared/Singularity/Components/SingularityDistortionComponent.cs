using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Singularity.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public class SingularityDistortionComponent : Component
    {
        public override string Name => "SingularityDistortion";

        [DataField("intensity")]
        private float _intensity = 0.25f;

        [DataField("falloff")]
        private float _falloff = 2;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Intensity
        {
            get => _intensity;
            set => this.SetAndDirtyIfChanged(ref _intensity, value);
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Falloff
        {
            get => _falloff;
            set => this.SetAndDirtyIfChanged(ref _falloff, value);
        }

        public override ComponentState GetComponentState()
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
        public SingularityDistortionComponentState(float intensity, float falloff)
        {
            Intensity = intensity;
            Falloff = falloff;
        }

        public float Intensity { get; }
        public float Falloff { get; }
    }
}
