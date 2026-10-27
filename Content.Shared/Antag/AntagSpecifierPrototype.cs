using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Antag;

/// <summary>
/// A more specific version of <see cref="AntagPrototype"/> which includes additional information that can vary between antags of the same type.
/// </summary>
/// <remarks>
/// Some of this should be moved to <see cref="AntagSelectionComponent"/> at a later date.
/// Specifically MinMax, PlayerRatio and LateJoin logic.
/// This would allow for greater control over spawning that a static prototype doesn't offer.
/// </remarks>
[Prototype]
[DataDefinition]
public sealed partial class AntagSpecifierPrototype : IPrototype, IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AntagSpecifierPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A list of antagonist roles that are used for selecting which players will be antagonists.
    /// </summary>
    [DataField]
    public List<ProtoId<AntagPrototype>> PrefRoles = new();

    /// <summary>
    /// Should we allow people who already have an antagonist role?
    /// </summary>
    [DataField]
    public AntagAcceptability MultiAntagSetting = AntagAcceptability.None;

    /// <summary>
    /// Whether or not players should be picked to inhabit this antag or not.
    /// If no players are left and <see cref="SpawnerPrototype"/> is set, it will make a ghost role.
    /// </summary>
    [DataField]
    public bool PickPlayer = true;

    /// <summary>
    /// A list of whitelisted jobs for this antagonist.
    /// Useful for antags that should only roll on a specific job, such as Malf AI.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public HashSet<ProtoId<JobPrototype>>? JobWhitelist;

    /// <summary>
    /// A list of blacklisted jobs for this antagonist.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public HashSet<ProtoId<JobPrototype>>? JobBlacklist;

    /// <remarks>
    /// Mostly just here for legacy compatibility and reducing boilerplate
    /// </remarks>
    [DataField]
    public bool AllowNonHumans;

    /// <summary>
    /// A whitelist for selecting which players can become this antag.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist for selecting which players can become this antag.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Components added to the player.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Components added to the player's mind.
    /// Do NOT use this to add role-type components. Add those as MindRoles instead
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry MindComponents = new();

    /// <summary>
    /// List of Mind Role Prototypes to be added to the player's mind.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<EntProtoId>? MindRoles;

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
/// Used by AntagSelectionSystem to indicate which types of antag roles are allowed to choose the same entity
/// For example, Thief HeadRev
/// </summary>
public enum AntagAcceptability
{
    /// <summary>
    /// Dont choose anyone who already has an antag role
    /// </summary>
    None,
    /// <summary>
    /// Dont choose anyone who has an exclusive antag role
    /// </summary>
    NotExclusive,
    /// <summary>
    /// Choose anyone
    /// </summary>
    All,
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

