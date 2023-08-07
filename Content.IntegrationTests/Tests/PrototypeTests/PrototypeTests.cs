using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.PrototypeTests;

public sealed class PrototypeTests
{
    /// <summary>
    /// This test writes all known server prototypes as yaml files, then validates that the result is valid yaml.
    /// Can help prevent instances where prototypes have bad C# default values.
    /// </summary>
    [Test]
    public async Task TestAllServerPrototypesAreSerializable()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var context = new PrototypeSaveTest.TestEntityUidContext();
        await SaveThenValidatePrototype(pairTracker.Pair.Server, "server", context);
        await pairTracker.CleanReturnAsync();
    }

    /// <summary>
    /// This test writes all known client prototypes as yaml files, then validates that the result is valid yaml.
    /// Can help prevent instances where prototypes have bad C# default values.
    /// </summary>
    [Test]
    public async Task TestAllClientPrototypesAreSerializable()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var context = new PrototypeSaveTest.TestEntityUidContext();
        await SaveThenValidatePrototype(pairTracker.Pair.Client, "client", context);
        await pairTracker.CleanReturnAsync();
    }

    public async Task SaveThenValidatePrototype(RobustIntegrationTest.IntegrationInstance instance, string instanceId,
        PrototypeSaveTest.TestEntityUidContext ctx)
    {
        var protoMan = instance.ResolveDependency<IPrototypeManager>();
        Dictionary<Type, Dictionary<string, HashSet<ErrorNode>>> errors = default!;
        await instance.WaitPost(() => errors = protoMan.ValidateAllPrototypesSerializable(ctx));

        if (errors.Count == 0)
            return;

        Assert.Multiple(() =>
        {
            foreach (var (kind, ids) in errors)
            {
                foreach (var (id, nodes) in ids)
                {
                    var msg = $"Error when validating {instanceId} prototype ({kind.Name}, {id}). Errors: \n";
                    foreach (var errorNode in nodes)
                    {
                        msg += $" - {errorNode.ErrorReason}\n";
                    }
                    Assert.Fail(msg);
                }
            }
        });
    }
}
