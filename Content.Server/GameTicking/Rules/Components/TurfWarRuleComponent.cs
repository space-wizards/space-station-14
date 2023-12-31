using Content.Server.GameTicking.Rules;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule for the turf war sub-gamemode.
/// Each department can have 1 member selected as a turf tagger.
/// </summary>
[RegisterComponent, Access(typeof(TurfWarRuleSystem))]
public sealed partial class TurfWarRuleComponent : Component
{
    /// <summary>
    /// Minds of the turf taggers using this rule, for each department.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DepartmentPrototype>, EntityUid> Minds = new();

    /// <summary>
    /// Station the turf war is being fought on.
    /// </summary>
    [DataField]
    public EntityUid? Station;

    /// <summary>
    /// Antagonist prototype to use.
    /// Taggers have an antag prototype so people can opt out of being one, not because they are antagonists.
    /// </summary>
    [DataField]
    public ProtoId<AntagPrototype> Antag = "TurfTagger";

    /// <summary>
    /// Greeting sound for turf taggers.
    /// </summary>
    [DataField]
    public SoundSpecifier? GreetingSound;

    /// <summary>
    /// The gear turf taggers are given on spawn.
    /// </summary>
    [DataField]
    public List<EntProtoId> StartingGear = new()
    {
        "SprayPainter",
        "ClothingHeadBandSkull"
    };

    /// <summary>
    /// When starting the gamemode, the number of departments is the player count divided by this number.
    /// If it is below <c>Min</c> then the gamemode is cancelled.
    /// If it is above <c>Max</c> then it is clamped.
    /// </summary>
    [DataField(required: true)]
    public int PlayersPerTagger;

    /// <summary>
    /// Minimum players to start a turf war with.
    /// If there were not enough players then the gamemode is cancelled.
    /// </summary>
    [DataField(required: true)]
    public int Min;

    /// <summary>
    /// Maximum players to start a turf war with.
    /// </summary>
    [DataField(required: true)]
    public int Max;
}
