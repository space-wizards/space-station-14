using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class SingularityDistortionComponent : Component
    {
        // TODO: use access and remove this funny stuff
        [DataField("intensity")]
        private float _intensity = 31.25f;

        [DataField("falloffPower")]
        private float _falloffPower = MathF.Sqrt(2f);

        [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public float Intensity
        {
            get => _intensity;
            set => this.SetAndDirtyIfChanged(ref _intensity, value);
        }

        [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public float FalloffPower
        {
            get => _falloffPower;
            set => this.SetAndDirtyIfChanged(ref _falloffPower, value);
        }
    }
}
