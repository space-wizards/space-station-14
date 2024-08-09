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
    /// The objective to give each turf tagger.
    /// </summary>
    [DataField]
    public EntProtoId Objective = "TurfTaggingObjective";
}
