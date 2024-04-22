using Content.Shared.Access;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype("job")]
    public sealed partial class JobPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("playTimeTracker", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<PlayTimeTrackerPrototype>))]
        public string PlayTimeTracker { get; private set; } = string.Empty;

        [DataField("supervisors")]
        public string Supervisors { get; private set; } = "nobody";

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name { get; private set; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField("description")]
        public string? Description { get; private set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string? LocalizedDescription => Description is null ? null : Loc.GetString(Description);

        [DataField("requirements")]
        public HashSet<JobRequirement>? Requirements;

        [DataField("joinNotifyCrew")]
        public bool JoinNotifyCrew { get; private set; } = false;

        [DataField("requireAdminNotify")]
        public bool RequireAdminNotify { get; private set; } = false;

        [DataField("setPreference")]
        public bool SetPreference { get; private set; } = true;

        /// <summary>
        ///     Whether this job should show in the ID Card Console.
        ///     If set to null, it will default to SetPreference's value.
        /// </summary>
        [DataField]
        public bool? OverrideConsoleVisibility { get; private set; } = null;

        [DataField("canBeAntag")]
        public bool CanBeAntag { get; private set; } = true;

        /// <summary>
        ///     Whether this job is a head.
        ///     The job system will try to pick heads before other jobs on the same priority level.
        /// </summary>
        [DataField("weight")]
        public int Weight { get; private set; }

        /// <summary>
        /// How to sort this job relative to other jobs in the UI.
        /// Jobs with a higher value with sort before jobs with a lower value.
        /// If not set, <see cref="Weight"/> is used as a fallback.
        /// </summary>
        [DataField]
        public int? DisplayWeight { get; private set; }

        public int RealDisplayWeight => DisplayWeight ?? Weight;

        /// <summary>
        ///     A numerical score for how much easier this job is for antagonists.
        ///     For traitors, reduces starting TC by this amount. Other gamemodes can use it for whatever they find fitting.
        /// </summary>
        [DataField("antagAdvantage")]
        public int AntagAdvantage = 0;

        [DataField("startingGear", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
        public string? StartingGear { get; private set; }

        /// <summary>
        /// Use this to spawn in as a non-humanoid (borg, test subject, etc.)
        /// Starting gear will be ignored.
        /// If you want to just add special attributes to a humanoid, use AddComponentSpecial instead.
        /// </summary>
        [DataField("jobEntity", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? JobEntity = null;

        [DataField("icon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
        public string Icon { get; private set; } = "JobIconUnknown";

        [DataField("special", serverOnly: true)]
        public JobSpecial[] Special { get; private set; } = Array.Empty<JobSpecial>();

        [DataField("access")]
        public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> Access { get; private set; } = Array.Empty<ProtoId<AccessLevelPrototype>>();

        [DataField("accessGroups")]
        public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> AccessGroups { get; private set; } = Array.Empty<ProtoId<AccessGroupPrototype>>();

        [DataField("extendedAccess")]
        public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> ExtendedAccess { get; private set; } = Array.Empty<ProtoId<AccessLevelPrototype>>();

        [DataField("extendedAccessGroups")]
        public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> ExtendedAccessGroups { get; private set; } = Array.Empty<ProtoId<AccessGroupPrototype>>();
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
}
