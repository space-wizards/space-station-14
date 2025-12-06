using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// Means this entity is bound to silicon laws and can view them.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedSiliconLawSystem))]
public sealed partial class SiliconLawProviderComponent : Component
{
    /// <summary>
    /// The id of the lawset that is being provided.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SiliconLawsetPrototype>? Laws = string.Empty;

    /// <summary>
    /// Whether the laws for this provider should be fetched using an event on map init.
    /// Takes priority over <see cref="Laws"/>.
    /// </summary>
    [DataField]
    public bool FetchOnInit = false;

    /// <summary>
    /// Lawset created from the prototype id.
    /// Cached when getting laws and modified during an ion storm event and when emagged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SiliconLawset Lawset = new ();

    /// <summary>
    /// The sound that plays for the Silicon player
    /// when the law change is processed for the provider.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? LawUploadSound = new SoundPathSpecifier("/Audio/Misc/cryo_warning.ogg");

    /// <summary>
    /// Whether this silicon is subverted by an ion storm or emag.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Subverted = false;

    // Prevent cheat clients from seeing the laws of other players.
    public override bool SendOnlyToOwner => true;
}
