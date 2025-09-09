using Content.Shared.Access;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype]
    public sealed partial class JobPrototype : RolePrototype
    {
        [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<PlayTimeTrackerPrototype>))]
        public string PlayTimeTracker { get; private set; } = string.Empty;

        /// <summary>
        ///     Who is the supervisor for this job.
        /// </summary>
        [DataField]
        public string Supervisors { get; private set; } = "nobody";

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField]
        public string? Description { get; private set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string? LocalizedDescription => Description is null ? null : Loc.GetString(Description);

        /// <summary>
        ///     When true - the station will have announcement about arrival of this player.
        /// </summary>
        [DataField]
        public bool JoinNotifyCrew { get; private set; }

        /// <summary>
        ///     When true - the player will receive a message about importance of their job.
        /// </summary>
        [DataField]
        public bool RequireAdminNotify { get; private set; }

        /// <summary>
        ///     Should the selected traits be applied for this job?
        /// </summary>
        [DataField]
        public bool ApplyTraits { get; private set; } = true;

        /// <summary>
        ///     Whether this job should show in the ID Card Console.
        ///     If set to null, it will default to SetPreference's value.
        /// </summary>
        [DataField]
        public bool? OverrideConsoleVisibility { get; private set; }

        [DataField]
        public bool CanBeAntag { get; private set; } = true;

        /// <summary>
        ///     The "weight" or importance of this job. If this number is large, the job system will assign this job
        ///     before assigning other jobs.
        /// </summary>
        [DataField]
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
        ///     For traitors, reduces starting TC by this amount. Other game modes can use it for whatever they find fitting.
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
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? JobEntity;

        /// <summary>
        /// Entity to use as a preview in the lobby/character editor.
        /// Same restrictions as <see cref="JobEntity"/> apply.
        /// </summary>
        [DataField]
        public EntProtoId? JobPreviewEntity;

        [DataField(serverOnly: true)]
        public JobSpecial[] Special { get; private set; } = [];

        [DataField]
        public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> Access { get; private set; } = [];

        [DataField]
        public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> AccessGroups { get; private set; } = [];

        [DataField]
        public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> ExtendedAccess { get; private set; } = [];

        [DataField]
        public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> ExtendedAccessGroups { get; private set; } = [];
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

            return cmp != 0 ? cmp : string.Compare(x.ID, y.ID, StringComparison.Ordinal);
        }
    }
}
