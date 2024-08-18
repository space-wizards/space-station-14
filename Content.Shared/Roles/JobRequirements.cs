using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public static class JobRequirements
{
    public static bool TryRequirementsMet(
        JobPrototype job,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out List<FormattedMessage> requirementDetails,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile)
    {
        var sys = entManager.System<SharedRoleSystem>();
        var requirements = sys.GetJobRequirement(job);
        requirementDetails = new List<FormattedMessage>();
        if (requirements == null)
            return true;

        var success = true;
        foreach (var requirement in requirements)
        {
            success = requirement.Check(entManager,
                          protoManager,
                          profile,
                          playTimes,
                          out var reason)
                      && success;

            requirementDetails.Add(reason);
        }

        return success;
    }
}

/// <summary>
/// Abstract class for playtime and other requirements for role gates.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class JobRequirement
{
    [DataField]
    public bool Inverted;

    public abstract bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out FormattedMessage details);
}
