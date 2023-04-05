using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles
{
    /// <summary>
    /// Abstract class for playtime and other requirements for role gates.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class JobRequirement{}

    [UsedImplicitly]
    public sealed class DepartmentTimeRequirement : JobRequirement
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
    public sealed class RoleTimeRequirement : JobRequirement
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
    public sealed class OverallPlaytimeRequirement : JobRequirement
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
            Dictionary<string, TimeSpan> playTimes,
            [NotNullWhen(false)] out string? reason,
            IPrototypeManager prototypes)
        {
            reason = null;
            if (job.Requirements == null)
                return true;

            foreach (var requirement in job.Requirements)
            {
                if (!TryRequirementMet(requirement, playTimes, out reason, prototypes))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a string with the reason why a particular requirement may not be met.
        /// </summary>
        public static bool TryRequirementMet(
            JobRequirement requirement,
            Dictionary<string, TimeSpan> playTimes,
            [NotNullWhen(false)] out string? reason,
            IPrototypeManager prototypes)

        {
            reason = null;

            switch (requirement)
            {
                case DepartmentTimeRequirement deptRequirement:
                    var playtime = TimeSpan.Zero;

                    // Check all jobs' departments
                    var jobs = prototypes.Index<DepartmentPrototype>(deptRequirement.Department).Roles;
                    string proto;

                    // Check all jobs' playtime
                    foreach (var other in jobs)
                    {
                        // The schema is stored on the Job role but we want to explode if the timer isn't found anyway.
                        proto = prototypes.Index<JobPrototype>(other).PlayTimeTracker;

                        playTimes.TryGetValue(proto, out var otherTime);
                        playtime += otherTime;
                    }

                    var deptDiff = deptRequirement.Time.TotalMinutes - playtime.TotalMinutes;

                    if (!deptRequirement.Inverted)
                    {
                        if (deptDiff <= 0)
                            return true;

                        reason = Loc.GetString(
                            "role-timer-department-insufficient",
                            ("time", deptDiff),
                            ("department", Loc.GetString(deptRequirement.Department)));
                        return false;
                    }
                    else
                    {
                        if (deptDiff <= 0)
                        {
                            reason = Loc.GetString(
                                "role-timer-department-too-high",
                                ("time", -deptDiff),
                                ("department", Loc.GetString(deptRequirement.Department)));
                            return false;
                        }

                        return true;
                    }

                case OverallPlaytimeRequirement overallRequirement:
                    var overallTime = playTimes.GetValueOrDefault(PlayTimeTrackingShared.TrackerOverall);
                    var overallDiff = overallRequirement.Time.TotalMinutes - overallTime.TotalMinutes;

                    if (!overallRequirement.Inverted)
                    {
                        if (overallDiff <= 0 || overallTime >= overallRequirement.Time)
                            return true;

                        reason = Loc.GetString("role-timer-overall-insufficient", ("time", overallDiff));
                        return false;
                    }
                    else
                    {
                        if (overallDiff <= 0 || overallTime >= overallRequirement.Time)
                        {
                            reason = Loc.GetString("role-timer-overall-too-high", ("time", -overallDiff));
                            return false;
                        }

                        return true;
                    }

                case RoleTimeRequirement roleRequirement:
                    proto = roleRequirement.Role;

                    playTimes.TryGetValue(proto, out var roleTime);
                    var roleDiff = roleRequirement.Time.TotalMinutes - roleTime.TotalMinutes;

                    if (!roleRequirement.Inverted)
                    {
                        if (roleDiff <= 0)
                            return true;

                        reason = Loc.GetString(
                            "role-timer-role-insufficient",
                            ("time", roleDiff),
                            ("job", Loc.GetString(proto)));
                        return false;
                    }
                    else
                    {
                        if (roleDiff <= 0)
                        {
                            reason = Loc.GetString(
                                "role-timer-role-too-high",
                                ("time", -roleDiff),
                                ("job", Loc.GetString(proto)));
                            return false;
                        }


                        return true;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
