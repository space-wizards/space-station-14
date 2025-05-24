using System.Collections.Generic;
using System.Text;
using Content.Client.Implants;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Implants;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chameleon;

// Ensures all round start jobs have an associated chameleon loadout.
public sealed class ChameleonJobLoadoutTest : InteractionTest
{

    // Ensure all jobs have a chameleon clothing prototype
    [Test]
    public async Task CheckAllJobs()
    {
        var chamSystem = Pair.Client.EntMan.System<ChameleonControllerSystem>();

        var alljobs = ProtoMan.EnumeratePrototypes<JobPrototype>();

        // Job, references
        Dictionary<ProtoId<JobPrototype>, List<ProtoId<ChameleonOutfitPrototype>>> validJobs = new();

        // Only add stuff that actually has clothing! We don't want stuff like AI or borgs.
        foreach (var job in alljobs)
        {
            if (!chamSystem.IsValidJob(job))
                continue;

            validJobs.Add(job.ID, new());
        }

        var chameleons = ProtoMan.EnumeratePrototypes<ChameleonOutfitPrototype>();

        foreach (var chameleon in chameleons)
        {
            if (chameleon.Job == null)
                continue;

            // The job must exist
            Assert.That(validJobs.TryGetValue(chameleon.Job.Value, out _));

            validJobs[chameleon.Job.Value].Add(chameleon.ID);
        }

        var errorMessage = new StringBuilder();
        errorMessage.AppendLine("The following jobs have issues with their chameleonOutfit prototype(s):");
        var invalid = false;

        // All round start jobs have a chameleon loadout
        foreach (var job in validJobs)
        {
            if (job.Value.Count == 1)
                continue;

            // string builder but also + at the same time its pro trust
            errorMessage.AppendLine(job.Key + " has " + (job.Value.Count == 0 ? "no chameleonOutfit prototype." : $"too many chameleonOutfit references ({job.Value.Count}):"));
            foreach (var chameleon in job.Value)
            {
                errorMessage.AppendLine("   " + chameleon);
            }
            invalid = true;
        }

        if (!invalid)
            return;

        Assert.Fail(errorMessage.ToString());
    }

}
