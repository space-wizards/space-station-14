using Content.Shared.Access;
using Content.Shared.Guidebook;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
/// Describes information for a single job on the station.
/// </summary>
[Prototype]
public sealed partial class JobPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<PlayTimeTrackerPrototype> PlayTimeTracker = string.Empty;

    /// <summary>
    /// Who is the supervisor for this job.
    /// </summary>
    [DataField]
    public LocId Supervisors = "job-supervisors-nobody";

    /// <summary>
    /// The name of this job as displayed to players.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// The name of this job as displayed to players.
    /// </summary>
    [DataField]
    public string? Description;

    [ViewVariables(VVAccess.ReadOnly)]
    public string? LocalizedDescription => Description is null ? null : Loc.GetString(Description);

    /// <summary>
    /// Requirements for the job.
    /// </summary>
    [DataField, Access(typeof(SharedRoleSystem), Other = AccessPermissions.None)]
    public HashSet<JobRequirement>? Requirements;

    /// <summary>
    /// When true - the station will have announcement about arrival of this player.
    /// </summary>
    [DataField]
    public bool JoinNotifyCrew;

    /// <summary>
    /// When true - the player will recieve a message about importancy of their job.
    /// </summary>
    [DataField]
    public bool RequireAdminNotify;

    /// <summary>
    /// Should this job appear in preferences menu?
    /// </summary>
    [DataField]
    public bool SetPreference = true;

    /// <summary>
    /// Should the selected traits be applied for this job?
    /// </summary>
    [DataField]
    public bool ApplyTraits = true;

    /// <summary>
    /// Whether this job should show in the ID Card Console.
    /// If set to null, it will default to SetPreference's value.
    /// </summary>
    [DataField]
    public bool? OverrideConsoleVisibility;

    /// <summary>
    /// The "weight" or importance of this job. If this number is large, the job system will assign this job
    /// before assigning other jobs.
    /// </summary>
    [DataField]
    public int Weight;

    /// <summary>
    /// How to sort this job relative to other jobs in the UI.
    /// Jobs with a higher value with sort before jobs with a lower value.
    /// If not set, <see cref="Weight"/> is used as a fallback.
    /// </summary>
    [DataField]
    public int? DisplayWeight;

    public int RealDisplayWeight => DisplayWeight ?? Weight;

    /// <summary>
    /// A numerical score for how much easier this job is for antagonists.
    /// For traitors, reduces starting TC by this amount. Other gamemodes can use it for whatever they find fitting.
    /// </summary>
    [DataField]
    public int AntagAdvantage;

    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear { get; private set; }

    /// <summary>
    /// Use this to spawn in as a non-humanoid (borg, test subject, etc.)
    /// Starting gear will be ignored.
    /// If you want to just add special attributes to a humanoid, use AddComponentSpecial instead.
    /// </summary>
    [DataField]
    public EntProtoId? JobEntity;

    /// <summary>
    /// Entity to use as a preview in the lobby/character editor.
    /// Same restrictions as <see cref="JobEntity"/> apply.
    /// </summary>
    [DataField]
    public EntProtoId? JobPreviewEntity;

    [DataField]
    public ProtoId<JobIconPrototype> Icon = "JobIconUnknown";

    [DataField(serverOnly: true)]
    public JobSpecial[] Special { get; private set; } = Array.Empty<JobSpecial>();

    [DataField]
    public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> Access = Array.Empty<ProtoId<AccessLevelPrototype>>();

    [DataField]
    public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> AccessGroups = Array.Empty<ProtoId<AccessGroupPrototype>>();

    [DataField]
    public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> ExtendedAccess = Array.Empty<ProtoId<AccessLevelPrototype>>();

    [DataField]
    public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> ExtendedAccessGroups = Array.Empty<ProtoId<AccessGroupPrototype>>();

    [DataField]
    public bool Whitelisted;

    /// <summary>
    /// Optional list of guides associated with this role. If the guides are opened, the first entry in this list
    /// will be used to select the currently selected guidebook.
    /// </summary>
    [DataField]
    public List<ProtoId<GuideEntryPrototype>>? Guides;
}

/// <summary>
/// Sorts <see cref="JobPrototype"/>s appropriately for display in the UI,
/// respecting their <see cref="JobPrototype.Weight"/>.
/// </summary>
public sealed class JobUIComparer : IComparer<JobPrototype>
{
    public static readonly JobUIComparer Instance = new();

    public int Compare(JobPrototype? x, JobPrototype? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (ReferenceEquals(null, y))
            return 1;
        if (ReferenceEquals(null, x))
            return -1;

        var cmp = -x.RealDisplayWeight.CompareTo(y.RealDisplayWeight);
        if (cmp != 0)
            return cmp;
        return string.Compare(x.ID, y.ID, StringComparison.Ordinal);
    }
}
