using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.NPC.HTN;
using NUnit.Framework;
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

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var counts = new Dictionary<string, int>();

            foreach (var compound in protoManager.EnumeratePrototypes<HTNCompoundTask>())
            {
                Count(compound, counts);
                counts.Clear();
            }
        });

        await pool.CleanReturnAsync();
    }

    private static void Count(HTNCompoundTask compound, Dictionary<string, int> counts)
    {
        foreach (var branch in compound.Branches)
        {
            foreach (var task in branch.Tasks)
            {
                if (task is HTNCompoundTask compoundTask)
                {
                    var count = counts.GetOrNew(compound.ID);
                    count++;

                    Assert.That(count, Is.LessThan(50));
                    counts[compound.ID] = count;
                    Count(compoundTask, counts);
                }
            }
        }
    }
}
