using Content.Shared.Antag.Selectors;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Antag.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AntagSelectionComponent : Component
{
    /// <summary>
    /// Has the primary assignment of antagonists been handled yet?
    /// This is typically set to true at the start of antag assignment for a game rule.
    /// Note that this can be true even before all antags have been assigned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AssignmentHandled;

    /// <summary>
    /// Has the antagonists been preselected but yet to be fully assigned?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreSelectionsComplete;

    /// <summary>
    /// If true, players that late join into a round have a chance of being converted into antagonists for this game rule.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LateJoinAdditional;

    /// <summary>
    /// The antag specifiers for the antagonists
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public AntagCountSelector[] Antags = Array.Empty<AntagCountSelector>();

    /// <summary>
    /// Cached sessions of antag definitions and selected players.
    /// Players in this dict are not guaranteed to have been assigned the role yet, and may be removed if they fail to initialize as an antag.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<AntagSpecifierPrototype>, HashSet<ICommonSession>> PreSelectedSessions = new();

    // TODO make the source generator handle HashSet of tuple that contains EntityUid so this can be networked. Or end the suffering and make Lidgren itself handle this shit!
    /// <summary>
    /// The minds and original names of the players assigned to be antagonists, as well as their assigned antag.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagSpecifierPrototype>, HashSet<(EntityUid uid, string name)>> AssignedMinds = new();

    /// <summary>
    /// When the antag selection will occur.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AntagSelectionTime SelectionTime = AntagSelectionTime.RuleStarted;

    /// <summary>
    /// Locale id for the name of the antag.
    /// If this is set then the antag is listed in the round-end summary.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? AgentName;

    /// <summary>
    /// If the player is pre-selected but fails to spawn in (e.g. due to only having antag-immune jobs selected),
    /// should they be removed from the pre-selection list?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveUponFailedSpawn = true;
}

/// <remarks>
///     Regardless of this value, antags are only initialized after the game rule activates.
///     If a game rule does not have a delayed activation, the antag will be initialized at the same time as this enum.
///     Otherwise, it will not be initialized until the game rule becomes active.
/// </remarks>
[Serializable, NetSerializable]
public enum AntagSelectionTime : byte
{
    /// <summary>
    /// Antag roles are selected at <see cref="RulePlayerSpawningEvent"/>
    /// </summary>
    PrePlayerSpawn,

    /// <summary>
    /// Antag roles are selected at <see cref="RulePlayerJobsAssignedEvent"/>
    /// </summary>
    JobsAssigned,

    /// <summary>
    /// Antag roles are selected at <see cref="GameRuleStartedEvent"/>
    /// or <see cref="RulePlayerJobsAssignedEvent"/> if the game rule was started before spawning.
    /// This is the latest an antag can be selected.
    /// </summary>
    RuleStarted,

    /// <summary>
    /// Antag roles are *never* selected. Instead, this definition only makes ghost roles.
    /// </summary>
    Never,
}
