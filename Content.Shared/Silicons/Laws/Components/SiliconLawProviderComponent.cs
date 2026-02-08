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
    /// Will also be the lawset this entity gets when initialized.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<SiliconLawsetPrototype> Laws = "Crewsimov";

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

    /// <summary>
    /// The list of <see cref="SiliconLawBoundComponent"/> entities who take laws from this provider.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ExternalLawsets = new();

    // Prevent cheat clients from seeing the laws of other players.
    public override bool SendOnlyToOwner => true;
}
