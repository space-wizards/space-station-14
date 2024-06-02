using Robust.Client.Upload.Commands;
using Robust.Shared.Prototypes;
using Robust.Shared.Upload;

namespace Content.IntegrationTests.Tests.PrototypeTests;

public sealed class PrototypeUploadTest
{
    public const string IdA = "UploadTestPrototype";
    public const string IdB = "UploadTestPrototypeNoParent";

    private const string File = $@"
- type: entity
  parent: BaseStructure # BaseItem can cause AllItemsHaveSpritesTest to fail
  id: {IdA}

- type: entity
  id: {IdB}
";
    [Test]
    [TestOf(typeof(LoadPrototypeCommand))]
    public async Task TestFileUpload()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings {Connected = true});

        Assert.That(!pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdA, out _));
        Assert.That(!pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdA, out _));
        Assert.That(!pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdB, out _));
        Assert.That(!pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdB, out _));

        var protoLoad = pair.Client.ResolveDependency<IGamePrototypeLoadManager>();
        await pair.Client.WaitPost(() => protoLoad.SendGamePrototype(File));
        await pair.RunTicksSync(10);

        Assert.That(pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdA, out _));
        Assert.That(pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdA, out _));
        Assert.That(pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdB, out _));
        Assert.That(pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdB, out _));

        await pair.CleanReturnAsync();
    }
}
