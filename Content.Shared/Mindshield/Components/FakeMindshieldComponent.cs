using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FakeMindShieldComponent : Component
{

    /// <summary>
    /// The state of the Fake mindshield, if true the owning entity will display a mindshield effect on their job icon
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsEnabled { get; set; } = false;

    [DataField, AutoNetworkedField]
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";
}
