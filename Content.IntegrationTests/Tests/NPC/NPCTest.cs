using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.NPC.HTN;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.NPC;

[TestFixture]
public sealed class NPCTest
{
    [Test]
    public async Task CompoundRecursion()
    {
        var pool = await PoolManager.GetServerClient(new PoolSettings() { NoClient = true });
        var server = pool.Pair.Server;

        await server.WaitIdleAsync();

        var htnSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<HTNSystem>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var counts = new Dictionary<string, int>();

            foreach (var compound in protoManager.EnumeratePrototypes<HTNCompoundTask>())
            {
                Count(compound, counts, htnSystem);
                counts.Clear();
            }
        });

        await pool.CleanReturnAsync();
    }

    private static void Count(HTNCompoundTask compound, Dictionary<string, int> counts, HTNSystem htnSystem)
    {
        var compoundBranches = htnSystem.CompoundBranches[compound];

        for (var i = 0; i < compound.Branches.Count; i++)
        {
            foreach (var task in compoundBranches[i])
            {
                if (task is HTNCompoundTask compoundTask)
                {
                    var count = counts.GetOrNew(compound.ID);
                    count++;

                    Assert.That(count, Is.LessThan(50));
                    counts[compound.ID] = count;
                    Count(compoundTask, counts, htnSystem);
                }
            }
        }
    }
}
