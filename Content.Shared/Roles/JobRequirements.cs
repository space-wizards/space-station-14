using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Roles
{
    /// <summary>
    /// Abstract class for playtime and other requirements for role gates.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    [Serializable, NetSerializable]
    public abstract partial class JobRequirement{}

    [UsedImplicitly]
    [Serializable, NetSerializable]
    public sealed partial class DepartmentTimeRequirement : JobRequirement
    {
        /// <summary>
        /// Which department needs the required amount of time.
        /// </summary>
        [DataField("department", customTypeSerializer: typeof(PrototypeIdSerializer<DepartmentPrototype>))]
        public string Department = default!;

        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")] public TimeSpan Time;

        /// <summary>
        /// If true, requirement will return false if playtime above the specified time.
        /// </summary>
        /// <value>
        /// <c>False</c> by default.<br />
        /// <c>True</c> for invert general requirement
        /// </value>
        [DataField("inverted")] public bool Inverted;
    }

    [UsedImplicitly]
    [Serializable, NetSerializable]
    public sealed partial class RoleTimeRequirement : JobRequirement
    {
        /// <summary>
        /// What particular role they need the time requirement with.
        /// </summary>
        [DataField("role", customTypeSerializer: typeof(PrototypeIdSerializer<PlayTimeTrackerPrototype>))]
        public string Role = default!;

        /// <inheritdoc cref="DepartmentTimeRequirement.Time"/>
        [DataField("time")] public TimeSpan Time;

        /// <inheritdoc cref="DepartmentTimeRequirement.Inverted"/>
        [DataField("inverted")] public bool Inverted;
    }

    [UsedImplicitly]
    [Serializable, NetSerializable]
    public sealed partial class OverallPlaytimeRequirement : JobRequirement
    {
        /// <inheritdoc cref="DepartmentTimeRequirement.Time"/>
        [DataField("time")] public TimeSpan Time;

        /// <inheritdoc cref="DepartmentTimeRequirement.Inverted"/>
        [DataField("inverted")] public bool Inverted;
    }

    public static class JobRequirements
    {
        public static bool TryRequirementsMet(
            JobPrototype job,
            IReadOnlyDictionary<string, TimeSpan> playTimes,
            [NotNullWhen(false)] out FormattedMessage? reason,
            IEntityManager entManager,
            IPrototypeManager prototypes)
        {
            var sys = entManager.System<SharedRoleSystem>();
            var requirements = sys.GetJobRequirement(job);
            reason = null;
            if (requirements == null)
                return true;

            foreach (var requirement in requirements)
            {
                if (!TryRequirementMet(requirement, playTimes, out reason, entManager, prototypes))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a string with the reason why a particular requirement may not be met.
        /// </summary>
        public static bool TryRequirementMet(
            JobRequirement requirement,
            IReadOnlyDictionary<string, TimeSpan> playTimes,
            [NotNullWhen(false)] out FormattedMessage? reason,
            IEntityManager entManager,
            IPrototypeManager prototypes)
        {
            reason = null;

            switch (requirement)
            {
                case DepartmentTimeRequirement deptRequirement:
                    var playtime = TimeSpan.Zero;

                    // Check all jobs' departments
                    var department = prototypes.Index<DepartmentPrototype>(deptRequirement.Department);
                    var jobs = department.Roles;
                    string proto;

                    // Check all jobs' playtime
                    foreach (var other in jobs)
                    {
                        // The schema is stored on the Job role but we want to explode if the timer isn't found anyway.
                        proto = prototypes.Index<JobPrototype>(other).PlayTimeTracker;

                        playTimes.TryGetValue(proto, out var otherTime);
                        playtime += otherTime;
                    }

                    var deptDiffSpan = deptRequirement.Time - playtime;
                    var deptDiff = deptDiffSpan.TotalMinutes;

                    if (!deptRequirement.Inverted)
                    {
                        if (deptDiff <= 0)
                            return true;

                        var formattedDiff = ConvertTimeSpanToHoursMinutes(deptDiffSpan);
                        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                            "role-timer-department-insufficient",
                            ("time", formattedDiff),
                            ("department", Loc.GetString(deptRequirement.Department)),
                            ("departmentColor", department.Color.ToHex())));
                        return false;
                    }
                    else
                    {
                        if (deptDiff <= 0)
                        {
                            var formattedDiff = ConvertTimeSpanToHoursMinutes(-deptDiffSpan);
                            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                                "role-timer-department-too-high",
                                ("time", formattedDiff),
                                ("department", Loc.GetString(deptRequirement.Department)),
                                ("departmentColor", department.Color.ToHex())));
                            return false;
                        }

                        return true;
                    }

                case OverallPlaytimeRequirement overallRequirement:
                    var overallTime = playTimes.GetValueOrDefault(PlayTimeTrackingShared.TrackerOverall);
                    var overallDiffSpan = overallRequirement.Time - overallTime;
                    var overallDiff = overallDiffSpan.TotalMinutes;

                    if (!overallRequirement.Inverted)
                    {
                        if (overallDiff <= 0 || overallTime >= overallRequirement.Time)
                            return true;

                        var formattedDiff = ConvertTimeSpanToHoursMinutes(overallDiffSpan);
                        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                              "role-timer-overall-insufficient",
                              ("time", formattedDiff)));
                        return false;
                    }
                    else
                    {
                        if (overallDiff <= 0 || overallTime >= overallRequirement.Time)
                        {
                            var formattedDiff = ConvertTimeSpanToHoursMinutes(-overallDiffSpan);
                            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                                "role-timer-overall-too-high",
                                ("time", formattedDiff)));
                            return false;
                        }

                        return true;
                    }

                case RoleTimeRequirement roleRequirement:
                    proto = roleRequirement.Role;

                    playTimes.TryGetValue(proto, out var roleTime);
                    var roleDiffSpan = roleRequirement.Time - roleTime;
                    var roleDiff = roleDiffSpan.TotalMinutes;
                    var departmentColor = Color.Yellow;

                    if (entManager.EntitySysManager.TryGetEntitySystem(out SharedJobSystem? jobSystem))
                    {
                        var jobProto = jobSystem.GetJobPrototype(proto);

                        if (jobSystem.TryGetDepartment(jobProto, out var departmentProto))
                            departmentColor = departmentProto.Color;
                    }

                    if (!roleRequirement.Inverted)
                    {
                        if (roleDiff <= 0)
                            return true;

                        var formattedDiff = ConvertTimeSpanToHoursMinutes(roleDiffSpan);
                        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                            "role-timer-role-insufficient",
                            ("time", formattedDiff),
                            ("job", Loc.GetString(proto)),
                            ("departmentColor", departmentColor.ToHex())));
                        return false;
                    }
                    else
                    {
                        if (roleDiff <= 0)
                        {
                            var formattedDiff = ConvertTimeSpanToHoursMinutes(-roleDiffSpan);
                            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                                "role-timer-role-too-high",
                                ("time", formattedDiff),
                                ("job", Loc.GetString(proto)),
                                ("departmentColor", departmentColor.ToHex())));
                            return false;
                        }

                        return true;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private static string ConvertTimeSpanToHoursMinutes(TimeSpan timeSpan)
        {
            var hours = (int) timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;

            var formattedTimeLoc = Loc.GetString("role-timer-time-format", ("hours", hours), ("minutes", minutes));
            return formattedTimeLoc;
        }
    }
}
