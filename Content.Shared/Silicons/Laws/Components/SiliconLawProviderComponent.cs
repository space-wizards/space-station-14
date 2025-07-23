// Contains modifications made by Ronstation contributors, therefore this file is subject to MIT sublicensed with AGPL v3.0.
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Random; // Ronstation - modification.

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity which grants laws to a <see cref="SiliconLawBoundComponent"/>
/// </summary>
[RegisterComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed partial class SiliconLawProviderComponent : Component
{
    /// <summary>
    /// The id of the lawset that is being provided.
    /// </summary>
    [DataField] // Ronstation - modification.
    public ProtoId<SiliconLawsetPrototype>? Laws;

    // Ronstation - start of modifications.
    /// <summary>
    /// Weighted list of lawsets, superseeds Laws 
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> WeightedLaws = "DefaultLawsets";
    // Ronstation - end of modifications.

    /// <summary>
    /// Lawset created from the prototype id.
    /// Cached when getting laws and modified during an ion storm event and when emagged.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SiliconLawset? Lawset;

    /// <summary>
    /// The sound that plays for the Silicon player
    /// when the law change is processed for the provider.
    /// </summary>
    [DataField]
    public SoundSpecifier? LawUploadSound = new SoundPathSpecifier("/Audio/Misc/cryo_warning.ogg");

    /// <summary>
    /// Whether this silicon is subverted by an ion storm or emag.
    /// </summary>
    [DataField]
    public bool Subverted = false;

}
