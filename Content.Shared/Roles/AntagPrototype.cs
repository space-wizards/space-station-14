// Contains modifications made by Ronstation contributors, therefore this file is subject to MIT sublicensed with AGPL v3.0.
using Content.Shared.Guidebook;
using Content.Shared.Players.PlayTimeTracking; // Ronstation - modification.
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype; // Ronstation - modification.

namespace Content.Shared.Roles;

/// <summary>
///     Describes information for a single antag.
/// </summary>
[Prototype]
public sealed partial class AntagPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    // Ronstation - start of modifications.
    [DataField("playTimeTracker", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<PlayTimeTrackerPrototype>))]
    public string PlayTimeTracker { get; private set; } = string.Empty;

    [DataField("color")]
    public Color Color { get; private set; } = Color.Yellow;
    
    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);
    // Ronstation - end of modifications.

    /// <summary>
    ///     The name of this antag as displayed to players.
    /// </summary>
    [DataField("name")]
    public string Name { get; private set; } = "";

    /// <summary>
    ///     The antag's objective, shown in a tooltip in the antag preference menu or as a ghost role description.
    /// </summary>
    [DataField("objective", required: true)]
    public string Objective { get; private set; } = "";

    /// <summary>
    ///     Whether or not the antag role is one of the bad guys.
    /// </summary>
    [DataField("antagonist")]
    public bool Antagonist { get; private set; }

    /// <summary>
    ///     Whether or not the player can set the antag role in antag preferences.
    /// </summary>
    [DataField("setPreference")]
    public bool SetPreference { get; private set; }

    /// <summary>
    ///     Requirements that must be met to opt in to this antag role.
    /// </summary>
    // TODO ROLE TIMERS
    // Actually check if the requirements are met. Because apparently this is actually unused.
    [DataField, Access(typeof(SharedRoleSystem), Other = AccessPermissions.None)]
    public HashSet<JobRequirement>? Requirements;

    /// <summary>
    /// Optional list of guides associated with this antag. If the guides are opened, the first entry in this list
    /// will be used to select the currently selected guidebook.
    /// </summary>
    [DataField]
    public List<ProtoId<GuideEntryPrototype>>? Guides;
}
