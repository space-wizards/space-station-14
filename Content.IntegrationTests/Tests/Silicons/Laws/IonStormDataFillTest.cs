using System.Collections.Generic;
using Content.Shared.Silicons.Laws;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.Silicons.Laws;

[TestFixture]
public sealed class IonStormDataFillTest
{
    [Test]
    public async Task TestCycleDetection()
    {
        // Set up the server-client pair for integration testing
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var protoManager = server.ProtoMan;
            var random = server.ResolveDependency<IRobustRandom>();
            var entManager = server.EntMan;

            var failures = new List<string>();

            // Iterate through all IonStormDataFillPrototypes to check for recursion
            foreach (var proto in protoManager.EnumeratePrototypes<IonStormDataFillPrototype>())
            {
                // Create a temporary selector pointing to the current prototype
                var selector = new IonStormDataFill();

                // We use reflection to set the Target field as it typically has a private setter for data fields
                var targetField = typeof(IonStormDataFill).GetProperty("Target", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                targetField?.SetValue(selector, new ProtoId<IonStormDataFillPrototype>(proto.ID));

                // Perform a depth-first search to detect any cycles starting from this prototype
                if (IsRecursive(selector, random, protoManager, entManager, new HashSet<string>()))
                {
                    failures.Add(proto.ID);
                }
            }

            // Assert that no recursion was detected in any of the prototypes
            Assert.That(failures, Is.Empty, $"Recursion detected in the following IonStormDataFill prototypes: {string.Join(", ", failures)}");
        });

        await pair.CleanReturnAsync();
    }

    // Recursively checks if an IonLawSelector or its children reference an IonStormDataFill prototype already in seenIds.
    private bool IsRecursive(IonLawSelector selector, IRobustRandom random, IPrototypeManager protoManager, IEntityManager entManager, HashSet<string> seenIds)
    {
        // Handle the case where we encounter an IonStormDataFill selector which points to another prototype
        if (selector is IonStormDataFill fill)
        {
            // If the target is already in our path, we've detected a cycle
            if (seenIds.Contains(fill.Target))
                return true;

            // Stop if the prototype doesn't exist (not a recursion error, but shouldn't continue)
            if (!protoManager.TryIndex(fill.Target, out var target))
                return false;

            // Add the current target to the seen set for this branch
            seenIds.Add(fill.Target);

            // Check all targets within the referenced prototype
            foreach (var subSelector in target.Targets)
            {
                // We pass a new HashSet to ensure we only detect cycles within a specific branch,
                // avoiding false positives for multiple references to the same safe prototype.
                if (IsRecursive(subSelector, random, protoManager, entManager, new HashSet<string>(seenIds)))
                    return true;
            }
            return false;
        }

        // Handle joined dataset selectors by checking all their child selectors
        if (selector is JoinedDatasetFill joined)
        {
            foreach (var subSelector in joined.Selectors)
            {
                if (IsRecursive(subSelector, random, protoManager, entManager, seenIds))
                    return true;
            }
        }

        // Handle translation selectors by checking selectors within their arguments
        if (selector is TranslateFill translate)
        {
            foreach (var subSelector in translate.Args.Values)
            {
                if (IsRecursive(subSelector, random, protoManager, entManager, seenIds))
                    return true;
            }
        }

        // Other selector types (DatasetFill, ConstantFill, etc.) don't have sub-selectors and can't cause recursion
        return false;
    }
}
