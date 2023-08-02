using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.PrototypeTests;

public sealed class PrototypeTests
{
    /// <summary>
    /// This test writes all known prototypes as yaml files, then validates that the result is valid yaml.
    /// Can help prevent instances where prototypes have bad C# default values.
    /// </summary>
    [Test]
    public async Task TestAllPrototypesAreSerializable()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var context = new PrototypeSaveTest.TestEntityUidContext();
        Assert.Multiple(() =>
        {
            Validate(pairTracker.Pair.Server, "server", context);

            // TODO fix client serialization
            //Validate(pairTracker.Pair.Client, "client", context);
        });
        await pairTracker.CleanReturnAsync();
    }

    public void Validate(RobustIntegrationTest.IntegrationInstance instance, string instanceId,
        PrototypeSaveTest.TestEntityUidContext ctx)
    {
        var protoMan = instance.ResolveDependency<IPrototypeManager>();
        var errors = protoMan.ValidateAllPrototypesSerializable(ctx);

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
