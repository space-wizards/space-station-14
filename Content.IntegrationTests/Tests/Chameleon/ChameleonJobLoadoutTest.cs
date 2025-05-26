using System.Collections.Generic;
using System.Text;
using Content.Client.Implants;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Clothing;
using Content.Shared.Implants;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chameleon;

/// <summary>
/// Ensures all round <see cref="IsProbablyRoundStartJob">"round start jobs"</see> have an associated chameleon loadout.
/// </summary>
public sealed class ChameleonJobLoadoutTest : InteractionTest
{
    private readonly List<ProtoId<JobPrototype>> JobBlacklist =
    [

    ];

    [Test]
    public async Task CheckAllJobs()
    {
        var alljobs = ProtoMan.EnumeratePrototypes<JobPrototype>();

        // Job -> number of references
        Dictionary<ProtoId<JobPrototype>, int> validJobs = new();

        // Only add stuff that actually has clothing! We don't want stuff like AI or borgs.
        foreach (var job in alljobs)
        {
            if (!IsProbablyRoundStartJob(job) || JobBlacklist.Contains(job.ID))
                continue;

            validJobs.Add(job.ID, 0);
        }

        var chameleons = ProtoMan.EnumeratePrototypes<ChameleonOutfitPrototype>();

        foreach (var chameleon in chameleons)
        {
            if (chameleon.Job == null || !validJobs.ContainsKey(chameleon.Job.Value))
                continue;

            validJobs[chameleon.Job.Value] += 1;
        }

        var errorMessage = new StringBuilder();
        errorMessage.AppendLine("The following job(s) have no chameleon prototype(s):");
        var invalid = false;

        // All round start jobs have a chameleon loadout
        foreach (var job in validJobs)
        {
            if (job.Value != 0)
                continue;

            errorMessage.AppendLine(job.Key + " has no chameleonOutfit prototype.");
            invalid = true;
        }

        if (!invalid)
            return;

        Assert.Fail(errorMessage.ToString());
    }

    /// <summary>
    /// Best guess at what a "round start" job is.
    /// </summary>
    private bool IsProbablyRoundStartJob(JobPrototype job)
    {
        return job.StartingGear != null && ProtoMan.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID));
    }

}
