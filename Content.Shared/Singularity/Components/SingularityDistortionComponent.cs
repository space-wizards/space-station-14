using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
namespace Content.Shared.Singularity.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class SingularityDistortionComponent : Component
    {
        [DataField("intensity")]
        private float _intensity = 31.25f;

        [DataField("falloffPower")]
        private float _falloffPower = MathF.Sqrt(2f);

        [ViewVariables(VVAccess.ReadWrite)]
        public float Intensity
        {
            get => _intensity;
            set => this.SetAndDirtyIfChanged(ref _intensity, value);
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float FalloffPower
        {
            get => _falloffPower;
            set => this.SetAndDirtyIfChanged(ref _falloffPower, value);
        }

        public override ComponentState GetComponentState()
        {
            return new SingularityDistortionComponentState(Intensity, FalloffPower);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SingularityDistortionComponentState state)
            {
                return;
            }

            Intensity = state.Intensity;
            FalloffPower = state.Falloff;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SingularityDistortionComponentState : ComponentState
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
