using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FakeMindShieldComponent : Component
{

    [DataField, AutoNetworkedField]
    public bool IsEnabled { get; set; }

    [DataField, AutoNetworkedField]
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";
}
