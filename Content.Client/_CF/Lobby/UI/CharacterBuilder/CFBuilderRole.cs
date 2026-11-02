using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.CF.Lobby.UI.CharacterBuilder;

/// <summary>The role loadout the character builder is editing for the preferred or overridden job.</summary>
public static class CFBuilderRole
{
    public static bool TryGet(
        HumanoidCharacterProfile profile,
        JobPrototype? jobOverride,
        ICommonSession? session,
        IPrototypeManager prototypes,
        out RoleLoadoutPrototype roleProto,
        out RoleLoadout roleLoadout)
    {
        roleProto = default!;
        roleLoadout = default!;
        if (session == null)
            return false;

        var job = jobOverride ?? PreferredJob(profile, prototypes);
        var roleId = LoadoutSystem.GetJobPrototype(job.ID);
        if (!prototypes.TryIndex<RoleLoadoutPrototype>(roleId, out var resolved) || resolved == null)
            return false;

        roleProto = resolved;
        roleLoadout = profile.GetLoadoutOrDefault(
            roleId,
            session,
            profile.Species,
            IoCManager.Resolve<IEntityManager>(),
            prototypes);
        return true;
    }

    public static JobPrototype PreferredJob(HumanoidCharacterProfile profile, IPrototypeManager prototypes)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(pair => pair.Value == JobPriority.High).Key;
        var jobId = string.IsNullOrEmpty(highPriorityJob) ? GameTicker.FallbackOverflowJob : highPriorityJob;
        return prototypes.Index<JobPrototype>(jobId);
    }
}
