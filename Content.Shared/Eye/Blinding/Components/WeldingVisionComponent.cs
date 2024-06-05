using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components
{
    [RegisterComponent]
    [NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class WeldingVisionComponent : Component
    {
        [DataField, AutoNetworkedField]
        public float InnerDiameter = 2.0f;
        [DataField, AutoNetworkedField]
        public float OuterDiameter = 6.0f;

    }
}
