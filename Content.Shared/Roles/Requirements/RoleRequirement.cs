using System.Diagnostics.CodeAnalysis;
using Content.Shared.Job;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles.Requirements;

public static class RoleRequirementStatics
{
    public static bool TryJobRequirementsMet(
        ProtoId<JobPrototype> job,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile)
    {
        return TryRequirementsMet(
            entManager.System<SharedRoleSystem>().GetJobRequirement(job),
            playTimes,
            out reason,
            entManager,
            protoManager,
            profile
        );
    }

    public static bool TryRequirementsMet(
        HashSet<RoleRequirement>? requirements,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out FormattedMessage? reason,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile
    )
    {
        reason = null;

        if (requirements is null)
            return true;

        foreach (var requirement in requirements)
        {
            if (!requirement.Check(entManager, protoManager, profile, playTimes, out reason))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Abstract class for playtime and other requirements for role gates.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class RoleRequirement
{
    [DataField]
    public bool Inverted;

    public abstract bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason);
}
