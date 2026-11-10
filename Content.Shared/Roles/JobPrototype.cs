using System.Diagnostics.CodeAnalysis;
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
/// Sorts <see cref="JobPrototype"/>s appropriately for display using a map's job weighting profile.
/// </summary>
public sealed class JobUIComparer : IComparer<JobPrototype>
{
    private readonly IReadOnlyDictionary<ProtoId<JobPrototype>, int> _weights;

    private JobUIComparer(IReadOnlyDictionary<ProtoId<JobPrototype>, int> weights)
    {
        _weights = weights;
    }

    /// <summary>
    /// Creates a comparer when the global fallback profile exists.
    /// Without one, callers should retain the source order rather than sorting jobs.
    /// </summary>
    public static bool TryCreate(
        IPrototypeManager prototypes,
        ProtoId<JobWeightPrototype>? jobWeights,
        [NotNullWhen(true)] out JobUIComparer? comparer)
    {
        if (!prototypes.TryIndex(JobWeightPrototype.Default, out var defaultProfile))
        {
            comparer = null;
            return false;
        }

        var weights = new Dictionary<ProtoId<JobPrototype>, int>(defaultProfile.Weights);
        if (jobWeights != null && prototypes.TryIndex(jobWeights.Value, out var mapProfile))
        {
            foreach (var (job, weight) in mapProfile.Weights)
            {
                weights[job] = weight;
            }
        }

        comparer = new JobUIComparer(weights);
        return true;
    }

    /// <summary>
    /// Gets the configured display weight for a job, if one exists.
    /// </summary>
    public int? GetWeight(JobPrototype job)
    {
        return _weights.TryGetValue(job.ID, out var weight) ? weight : null;
    }

    public int Compare(JobPrototype? x, JobPrototype? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (ReferenceEquals(null, y))
            return 1;
        if (ReferenceEquals(null, x))
            return -1;

        var xWeight = GetWeight(x);
        var yWeight = GetWeight(y);
        if (xWeight == null || yWeight == null)
        {
            return 0;
        }

        var cmp = -xWeight.Value.CompareTo(yWeight.Value);
        if (cmp != 0)
            return cmp;
        return string.Compare(x.ID, y.ID, StringComparison.Ordinal);
    }
}
