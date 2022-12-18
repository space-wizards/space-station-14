using Content.Shared.Access;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single job on the station.
    /// </summary>
    [Prototype("job")]
    public sealed class JobPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("playTimeTracker", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<PlayTimeTrackerPrototype>))]
        public string PlayTimeTracker { get; } = string.Empty;

        [DataField("supervisors")]
        public string Supervisors { get; } = "nobody";

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        /// <summary>
        ///     The name of this job as displayed to players.
        /// </summary>
        [DataField("description")]
        public string? Description { get; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string? LocalizedDescription => Description is null ? null : Loc.GetString(Description);

        [DataField("requirements")]
        public HashSet<JobRequirement>? Requirements;

        [DataField("joinNotifyCrew")]
        public bool JoinNotifyCrew { get; } = false;

        [DataField("requireAdminNotify")]
        public bool RequireAdminNotify { get; } = false;

        [DataField("setPreference")]
        public bool SetPreference { get; } = true;

        [DataField("canBeAntag")]
        public bool CanBeAntag { get; } = true;

        /// <summary>
        ///     Whether this job is a head.
        ///     The job system will try to pick heads before other jobs on the same priority level.
        /// </summary>
        [DataField("weight")]
        public int Weight { get; private set; }

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

        [DataField("icon")] public string Icon { get; } = string.Empty;

        [DataField("special", serverOnly:true)]
        public JobSpecial[] Special { get; private set; } = Array.Empty<JobSpecial>();

        [DataField("access", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
        public IReadOnlyCollection<string> Access { get; } = Array.Empty<string>();

        [DataField("accessGroups", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessGroupPrototype>))]
        public IReadOnlyCollection<string> AccessGroups { get; } = Array.Empty<string>();

        [DataField("extendedAccess", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
        public IReadOnlyCollection<string> ExtendedAccess { get; } = Array.Empty<string>();

        [DataField("extendedAccessGroups", customTypeSerializer: typeof(PrototypeIdListSerializer<AccessGroupPrototype>))]
        public IReadOnlyCollection<string> ExtendedAccessGroups { get; } = Array.Empty<string>();
    }
}
