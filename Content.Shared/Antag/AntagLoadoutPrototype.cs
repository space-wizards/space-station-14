using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Antag;

[Prototype]
public sealed partial class AntagLoadoutPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AntagLoadoutPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    //todo: find out how to do this with minimal boilerplate: filler department, maybe?
    //public HashSet<ProtoId<JobPrototype>> JobBlacklist = new()

    /// <remarks>
    /// Mostly just here for legacy compatibility and reducing boilerplate
    /// </remarks>
    [DataField]
    public bool AllowNonHumans = true;

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
    public ComponentRegistry AddComponents = new();

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
