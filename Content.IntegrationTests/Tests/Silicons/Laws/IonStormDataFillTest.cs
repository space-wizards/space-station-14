using System.Collections.Generic;
using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.Silicons.Laws;

[TestFixture]
public sealed class IonStormDataFillTest
{
    // Test if Select handles recursion correctly by calling it with a seenIds set
    [Test]
    public async Task TestCycleDetection()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var protoManager = server.ProtoMan;
            var random = server.ResolveDependency<IRobustRandom>();
            var entManager = server.EntMan;

            var selector = new IonStormDataFill();

            // Set Target using reflection since it has a private setter
            var targetField = typeof(IonStormDataFill).GetProperty("Target", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            targetField?.SetValue(selector, new ProtoId<IonStormDataFillPrototype>("RecursiveTarget"));

            var seenIds = new HashSet<string> { "RecursiveTarget" };

            var result = selector.Select(random, protoManager, entManager, seenIds);
            Assert.That(result, Is.Null, "IonStormDataFill.Select should return null when recursion is detected.");
        });

        await pair.CleanReturnAsync();
    }
}
