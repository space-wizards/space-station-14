using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FakeMindShieldImplantComponent : Component
{
    [DataField]
    public EntProtoId Action = "FakeMindShieldToggleAction";

    [DataField]
    public EntityUid? ActionEntity;
}
