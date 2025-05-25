using Content.Shared.StatusIcon;
using Content.Shared.Tag;
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

    /// <summary>
    /// The Security status icon displayed to the security officer. Should be a duplicate of the one the mindshield uses since it's spoofing that
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";

    /// <summary>
    /// This tag should be placed on the fake mindshield action so there is a way to easily identify it.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> MindShieldActionTag = "FakeMindShieldImplant";
}
