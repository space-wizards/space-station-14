using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Corvax.HiddenDescription;

/// <summary>
/// A component that shows players with specific roles or jobs additional information about entities
/// </summary>

[RegisterComponent, Access(typeof(HiddenDescriptionSystem))]
public sealed partial class HiddenDescriptionComponent : Component
{
    [DataField(required: true)]
    public List<HiddenDescriptionEntry> Entries = new();

    /// <summary>
    /// Prioritizing the location of classified information in an inspection
    /// </summary>
    [DataField]
    public int PushPriority = 1;
}

[DataDefinition, Serializable]
public readonly partial record struct HiddenDescriptionEntry()
{
    /// <summary>
    /// Locale string with hidden description
    /// </summary>
    [DataField(required: true)]
    public LocId Label { get; init; } = default!;

    /// <summary>
    /// A player's mind must pass a whitelist check to receive hidden information
    /// </summary>
    [DataField]
    public EntityWhitelist WhitelistMind { get; init; } = new();

    /// <summary>
    /// A player's body must pass a whitelist check to receive hidden information
    /// </summary>
    [DataField]
    public EntityWhitelist WhitelistBody { get; init; } = new();

    /// <summary>
    /// The player's mind has to have some job role to access the hidden information
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> JobRequired { get; init; } = new();

    /// <summary>
    /// If true, the player needs to go through and whitelist, and have some job. By default, at least one successful checks is sufficient.
    /// </summary>
    [DataField]
    public bool NeedAllCheck { get; init; } = false;
}