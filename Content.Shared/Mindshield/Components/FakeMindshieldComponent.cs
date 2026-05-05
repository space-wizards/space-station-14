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
    /// This tag should be placed on the fake mindshield action so there is a way to easily identify which action goes with which fake mindshield. Of course, this implies that no two similar fake mindshields should be on the same entity at the same time.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> FakeMindShieldImplantTag = "FakeMindShieldImplant";

    /// <summary>
    /// This fake mindshield will only overwrite the mindshield visual of mindshields-and-such with lower priority
    /// </summary>
    [DataField]
    public int VisualPriority = 1;

    /// <summary>
    /// Makes it so chameleon implants affect this fake mindshield. Is off by default on changeling fake mindshields, for instance.
    /// </summary>
    [DataField]
    public bool ChameleonControllable = true;
}
