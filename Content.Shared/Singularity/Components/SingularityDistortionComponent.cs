using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    [Access(typeof(SharedSingularitySystem))]
    public sealed partial class SingularityDistortionComponent : Component
    {
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public float Intensity = 31.25f;

        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public float FalloffPower = MathF.Sqrt(2f);
    }
}
