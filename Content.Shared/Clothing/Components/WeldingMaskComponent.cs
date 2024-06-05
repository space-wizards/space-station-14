using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class WeldingMaskComponent : Component
    {
        [DataField]
        public float InnerDiameter = 2.0f;
        [DataField]
        public float OuterDiameter = 6.0f;

        public EntityUid Equipee;

        public bool Folded;
    }
}
