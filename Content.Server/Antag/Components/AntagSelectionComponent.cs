using Content.Server.Administration.Systems;
using Content.Shared.Antag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

[RegisterComponent, Access(typeof(AntagSelectionSystem), typeof(AdminVerbSystem))]
public sealed partial class AntagSelectionComponent : Component
{
    /// <summary>
    /// Has the primary assignment of antagonists finished yet?
    /// </summary>
    [ViewVariables]
    public bool AssignmentComplete;

    /// <summary>
    /// Has the antagonists been preselected but yet to be fully assigned?
    /// </summary>
    [ViewVariables]
    public bool PreSelectionsComplete;

    /// <summary>
    /// The antag specifiers for the antagonists
    /// </summary>
    // TODO: To Dict with keys being prototypes, and values being spawn count interface
    [DataField]
    public HashSet<ProtoId<AntagSpecifierPrototype>> Antags = new();

    /// <summary>
    /// Cached sessions of players who are chosen by prototype. Used so we don't have to rebuild the pool multiple times in a tick.
    /// Is not serialized.
    /// </summary>
    public  Dictionary<ProtoId<AntagSpecifierPrototype>, HashSet<ICommonSession>> AssignedSessions = new();

    /// <summary>
    /// The minds and original names of the players assigned to be antagonists, as well as their assigned antag.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagSpecifierPrototype>, HashSet<(EntityUid uid, string name)>> AssignedMinds = new();

    /// <summary>
    /// When the antag selection will occur.
    /// </summary>
    [DataField]
    public AntagSelectionTime SelectionTime = AntagSelectionTime.PostPlayerSpawn;

    // TODO: Maybe combine this with AssignedSessions?
    /// <summary>
    /// Cached sessions of antag definitions and selected players. Players in this dict are not guaranteed to have been assigned the role yet.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagSpecifierPrototype>, HashSet<ICommonSession>> PreSelectedSessions = new();

    /// <summary>
    /// Locale id for the name of the antag.
    /// If this is set then the antag is listed in the round-end summary.
    /// </summary>
    [DataField]
    public LocId? AgentName;

    /// <summary>
    /// If the player is pre-selected but fails to spawn in (e.g. due to only having antag-immune jobs selected),
    /// should they be removed from the pre-selection list?
    /// </summary>
    [DataField]
    public bool RemoveUponFailedSpawn = true;
}
