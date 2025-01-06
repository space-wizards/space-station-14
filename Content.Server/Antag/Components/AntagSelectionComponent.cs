using Content.Server.Administration.Systems;
using Content.Shared.Antag;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

[RegisterComponent, Access(typeof(AntagSelectionSystem), typeof(AdminVerbSystem))]
public sealed partial class AntagSelectionComponent : Component
{
    /// <summary>
    /// Has the primary selection of antagonists finished yet?
    /// </summary>
    [DataField]
    public bool SelectionsComplete;

    /// <summary>
    /// The definitions for the antagonists
    /// </summary>
    [DataField]
    public List<AntagSelectionDefinition> Definitions = new();

    /// <summary>
    /// The minds and original names of the players selected to be antagonists.
    /// </summary>
    [DataField]
    public List<(EntityUid, string)> SelectedMinds = new();

    /// <summary>
    /// When the antag selection will occur.
    /// </summary>
    [DataField]
    public AntagSelectionTime SelectionTime = AntagSelectionTime.PostPlayerSpawn;

    /// <summary>
    /// Cached sessions of players who are chosen. Used so we don't have to rebuild the pool multiple times in a tick.
    /// Is not serialized.
    /// </summary>
    public HashSet<ICommonSession> SelectedSessions = new();

    /// <summary>
    /// Locale id for the name of the antag.
    /// If this is set then the antag is listed in the round-end summary.
    /// </summary>
    [DataField]
    public LocId? AgentName;
}

[DataDefinition]
public partial struct AntagSelectionDefinition()
{
    /// <summary>
    /// A list of antagonist roles that are used for selecting which players will be antagonists.
    /// </summary>
    [DataField]
    public List<ProtoId<AntagPrototype>> PrefRoles = new();

    /// <summary>
    /// Fallback for <see cref="PrefRoles"/>. Useful if you need multiple role preferences for a team antagonist.
    /// </summary>
    [DataField]
    public List<ProtoId<AntagPrototype>> FallbackRoles = new();

    /// <summary>
    /// Should we allow people who already have an antagonist role?
    /// </summary>
    [DataField]
    public AntagAcceptability MultiAntagSetting = AntagAcceptability.None;

    /// <summary>
    /// The minimum number of this antag.
    /// </summary>
    [DataField]
    public int Min = 1;

    /// <summary>
    /// The maximum number of this antag.
    /// </summary>
    [DataField]
    public int Max = 1;

    /// <summary>
    /// A range used to randomly select <see cref="Min"/>
    /// </summary>
    [DataField]
    public MinMax? MinRange;

    /// <summary>
    /// A range used to randomly select <see cref="Max"/>
    /// </summary>
    [DataField]
    public MinMax? MaxRange;

    /// <summary>
    /// a player to antag ratio: used to determine the amount of antags that will be present.
    /// </summary>
    [DataField]
    public int PlayerRatio = 10;

    /// <summary>
    /// Whether or not players should be picked to inhabit this antag or not.
    /// If no players are left and <see cref="SpawnerPrototype"/> is set, it will make a ghost role.
    /// </summary>
    [DataField]
    public bool PickPlayer = true;

    /// <summary>
    /// If true, players that latejoin into a round have a chance of being converted into antagonists.
    /// </summary>
    [DataField]
    public bool LateJoinAdditional = false;

    //todo: find out how to do this with minimal boilerplate: filler department, maybe?
    //public HashSet<ProtoId<JobPrototype>> JobBlacklist = new()

    /// <remarks>
    /// Mostly just here for legacy compatibility and reducing boilerplate
    /// </remarks>
    [DataField]
    public bool AllowNonHumans = false;

    /// <summary>
    /// A whitelist for selecting which players can become this antag.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist for selecting which players can become this antag.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Components added to the player.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Components added to the player's mind.
    /// Do NOT use this to add role-type components. Add those as MindRoles instead
    /// </summary>
    [DataField]
    public ComponentRegistry MindComponents = new();

    /// <summary>
    /// List of Mind Role Prototypes to be added to the player's mind.
    /// </summary>
    [DataField]
    public List<ProtoId<EntityPrototype>>? MindRoles;

    /// <summary>
    /// A set of starting gear that's equipped to the player.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    /// <summary>
    /// A list of role loadouts, from which a randomly selected one will be equipped.
    /// </summary>
    [DataField]
    public List<ProtoId<RoleLoadoutPrototype>>? RoleLoadout;

    /// <summary>
    /// A briefing shown to the player.
    /// </summary>
    [DataField]
    public BriefingData? Briefing;

    /// <summary>
    /// A spawner used to defer the selection of this particular definition.
    /// </summary>
    /// <remarks>
    /// Not the cleanest way of doing this code but it's just an odd specific behavior.
    /// Sue me.
    /// </remarks>
    [DataField]
    public EntProtoId? SpawnerPrototype;
}

/// <summary>
/// Contains data used to generate a briefing.
/// </summary>
[DataDefinition]
public partial struct BriefingData
{
    /// <summary>
    /// The text shown
    /// </summary>
    [DataField]
    public LocId? Text;

    /// <summary>
    /// The color of the text.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// The sound played.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
