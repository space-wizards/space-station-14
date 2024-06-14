using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class WeldingMaskComponent : Component
    {
        [DataField]
        public float InnerDiameter = 2.0f;
        [DataField]
        public float OuterDiameter = 6.0f;

        [DataField, AutoNetworkedField]
        public EntityUid? ToggleActionEntity;

        [DataField, AutoNetworkedField]
        public EntProtoId ToggleAction = "ActionToggleWelding";

        public EntityUid Equipee;

        public bool Folded;

        public bool ToggledThisFrame = false;
    }
}
