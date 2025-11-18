#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
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
        await using var pair = await PoolManager.GetServerClient();
        var context = new PrototypeSaveTest.TestEntityUidContext();
        await SaveThenValidatePrototype(pair.Server, "server", context);
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// This test writes all known client prototypes as yaml files, then validates that the result is valid yaml.
    /// Can help prevent instances where prototypes have bad C# default values.
    /// </summary>
    [Test]
    public async Task TestAllClientPrototypesAreSerializable()
    {
        await using var pair = await PoolManager.GetServerClient();
        var context = new PrototypeSaveTest.TestEntityUidContext();
        await SaveThenValidatePrototype(pair.Client, "client", context);
        await pair.CleanReturnAsync();
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

    /// <summary>
    /// This test writes all known prototypes as yaml files, reads them again, then serializes them again.
    /// </summary>
    [Test]
    public async Task ServerPrototypeSaveLoadSaveTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var context = new PrototypeSaveTest.TestEntityUidContext();
        await SaveLoadSavePrototype(pair.Server, context);
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// This test writes all known prototypes as yaml files, reads them again, then serializes them again.
    /// </summary>
    [Test]
    public async Task ClientPrototypeSaveLoadSaveTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var context = new PrototypeSaveTest.TestEntityUidContext();
        await SaveLoadSavePrototype(pair.Client, context);
        await pair.CleanReturnAsync();
    }

    private async Task SaveLoadSavePrototype(
        RobustIntegrationTest.IntegrationInstance instance,
        PrototypeSaveTest.TestEntityUidContext ctx)
    {
        var protoMan = instance.ResolveDependency<IPrototypeManager>();
        var seriMan = instance.ResolveDependency<ISerializationManager>();
        await instance.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var kind in protoMan.EnumeratePrototypeKinds())
                {
                    foreach (var proto in protoMan.EnumeratePrototypes(kind))
                    {
                        var noException = TrySaveLoadSavePrototype(
                            seriMan,
                            protoMan,
                            kind,
                            proto,
                            ctx);

                        // This will probably throw an exception for each prototype of this kind.
                        // We want to avoid having tests crash because they run out of time.
                        if (!noException)
                            break;
                    }
                }
            });
        });
    }

    /// <returns>False if an exception was caught</returns>
    private bool TrySaveLoadSavePrototype(
        ISerializationManager seriMan,
        IPrototypeManager protoMan,
        Type kind,
        IPrototype proto,
        PrototypeSaveTest.TestEntityUidContext ctx)
    {
        DataNode first;
        DataNode second;

        try
        {
            first = seriMan.WriteValue(kind, proto, alwaysWrite: true, context:ctx);
        }
        catch (Exception e)
        {
            protoMan.TryGetMapping(kind, proto.ID, out var mapping);
            Assert.Fail($"Caught exception while writing {kind.Name} prototype {proto.ID}. Exception:\n{e}");
            return false;
        }

        object? obj;
        try
        {
            obj = seriMan.Read(kind, first, context:ctx);
        }
        catch (Exception e)
        {
            protoMan.TryGetMapping(kind, proto.ID, out var mapping);
            Assert.Fail($"Caught exception while re-reading {kind.Name} prototype {proto.ID}." +
                        $"\nException:\n{e}" +
                        $"\n\nOriginal yaml:\n{mapping}" +
                        $"\n\nWritten yaml:\n{first}");
            return false;
        }

        Assert.That(obj?.GetType(), Is.EqualTo(proto.GetType()));
        var deserialized = (IPrototype) obj!;

        try
        {
            second = seriMan.WriteValue(kind, deserialized, alwaysWrite: true, context:ctx);
        }
        catch (Exception e)
        {
            protoMan.TryGetMapping(kind, proto.ID, out var mapping);
            Assert.Fail($"Caught exception while re-writing {kind.Name} prototype {proto.ID}." +
                        $"\nException:\n{e}" +
                        $"\n\nOriginal yaml:\n{mapping}" +
                        $"\n\nWritten yaml:\n{first}");
            return false;
        }

        var diff = first.Except(second);
        if (diff == null || diff.IsEmpty)
            return true;

        protoMan.TryGetMapping(kind, proto.ID, out var orig);
        Assert.Fail($"Re-written {kind.Name} prototype {proto.ID} differs." +
                    $"\nYaml diff:\n{diff}" +
                    $"\n\nOriginal yaml:\n{orig}" +
                    $"\n\nWritten yaml:\n{first}" +
                    $"\n\nRe-written Yaml:\n{second}");
        return true;
    }
}
