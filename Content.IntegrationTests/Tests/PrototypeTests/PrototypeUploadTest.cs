using Content.Shared.Tag;
using Robust.Client.Upload.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Upload;

namespace Content.IntegrationTests.Tests.PrototypeTests;

public sealed class PrototypeUploadTest
{
    public const string IdA = "UploadTestPrototype";
    public const string IdB = $"{IdA}NoParent";
    public const string IdC = $"{IdA}Abstract";
    public const string IdD = $"{IdA}UploadedParent";

    private const string File = $@"
- type: entity
  parent: BaseStructure # BaseItem can cause AllItemsHaveSpritesTest to fail
  id: {IdA}

- type: entity
  id: {IdB}

- type: entity
  id: {IdC}
  abstract: true
  components:
  - type: Tag

- type: entity
  id: {IdD}
  parent: {IdC}
";

    [Test]
    [TestOf(typeof(LoadPrototypeCommand))]
    public async Task TestFileUpload()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings {Connected = true});
        var sCompFact = pair.Server.ResolveDependency<IComponentFactory>();
        var cCompFact = pair.Client.ResolveDependency<IComponentFactory>();

        Assert.That(!pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdA, out _));
        Assert.That(!pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdB, out _));
        Assert.That(!pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdC, out _));
        Assert.That(!pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdD, out _));

        Assert.That(!pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdA, out _));
        Assert.That(!pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdB, out _));
        Assert.That(!pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdC, out _));
        Assert.That(!pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdD, out _));

        var protoLoad = pair.Client.ResolveDependency<IGamePrototypeLoadManager>();
        await pair.Client.WaitPost(() => protoLoad.SendGamePrototype(File));
        await pair.RunTicksSync(10);

        Assert.That(pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdA, out var sProtoA));
        Assert.That(pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdB, out var sProtoB));
        Assert.That(!pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdC, out _));
        Assert.That(pair.Server.ProtoMan.TryIndex<EntityPrototype>(IdD, out var sProtoD));

        Assert.That(pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdA, out var cProtoA));
        Assert.That(pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdB, out var cProtoB));
        Assert.That(!pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdC, out _));
        Assert.That(pair.Client.ProtoMan.TryIndex<EntityPrototype>(IdD, out var cProtoD));

        // Arbitrarily choosing TagComponent to check that inheritance works for uploaded prototypes.

        await pair.Server.WaitPost(() =>
        {
            Assert.That(sProtoA!.TryGetComponent<TagComponent>(out _, sCompFact), Is.True);
            Assert.That(sProtoB!.TryGetComponent<TagComponent>(out _, sCompFact), Is.False);
            Assert.That(sProtoD!.TryGetComponent<TagComponent>(out _, sCompFact), Is.True);
        });

        await pair.Client.WaitPost(() =>
        {
            Assert.That(cProtoA!.TryGetComponent<TagComponent>(out _, cCompFact), Is.True);
            Assert.That(cProtoB!.TryGetComponent<TagComponent>(out _, cCompFact), Is.False);
            Assert.That(cProtoD!.TryGetComponent<TagComponent>(out _, cCompFact), Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
