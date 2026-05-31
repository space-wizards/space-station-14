using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// A toggleable fake mindshield that only produces mindshield visuals, but does not protect against anything.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMindShieldSystem), typeof(FakeMindShieldSystem))]
public sealed partial class FakeMindShieldComponent : Component
{

    /// <summary>
    /// The state of the Fake mindshield, if true the owning entity will display a mindshield effect on their job icon
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsEnabled;

    /// <summary>
    /// This tag should be placed on the fake mindshield action so there is a way to easily identify which action goes with which fake mindshield. Of course, this implies that no two similar fake mindshields should be on the same entity at the same time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype> ActionTag = "FakeMindShieldImplant";

    /// <summary>
    /// Makes it so chameleon implants affect this fake mindshield. Is off by default on changeling fake mindshields, for instance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ChameleonControllable = true;

    /// <summary>
    ///     Whether the fake mind shield is innate to the entity.
    ///     When added to an entity while this field is set to true, the entity itself will gain the action & UI necessary to change its voice.
    ///     When this field is set to false, then the entity with this component will be a provider (either through implanting or through wearing) of the voice masking abilities for another entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsInnate;

    /// <summary>
    ///     Action linked to the fake mindshield, used for innate components
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "FakeMindShieldToggleAction";
}
