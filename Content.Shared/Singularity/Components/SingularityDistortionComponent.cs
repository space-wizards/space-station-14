using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    [Access(typeof(SharedSingularitySystem))]
    public sealed partial class SingularityDistortionComponent : Component
    {
        [DataField]
        public float Intensity = 31.25f;

        [DataField]
        public float FalloffPower = MathF.Sqrt(2f);
    }
}
