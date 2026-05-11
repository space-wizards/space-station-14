using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Disposal;

public sealed class DisposalUnitInteractionTest : InteractionTest
{
    private static readonly EntProtoId DisposalUnit = "DisposalUnit";
    private static readonly EntProtoId TrashItem = "BrokenBottle";

    private const string TestDisposalUnitId = "TestDisposalUnit";

    [TestPrototypes]
    private static readonly string TestPrototypes = $@"
# A modified disposal unit with a 100% chance of a thrown item being inserted
- type: entity
  parent: {DisposalUnit.Id}
  id: {TestDisposalUnitId}
  components:
  - type: ThrowInsertContainer
    probability: 1
";

    /// <summary>
    /// Spawns a disposal unit, gives the player a trash item, and makes the
    /// player throw the item at the disposal unit.
    /// After a short delay, verifies that the thrown item is contained inside
    /// the disposal unit.
    /// </summary>
    [Test]
    public async Task ThrowItemIntoDisposalUnitTest()
    {
        var containerSys = Server.System<SharedContainerSystem>();

        // Spawn the target disposal unit
        var disposalUnit = await SpawnTarget(TestDisposalUnitId);

        // Give the player some trash to throw
        var trash = await PlaceInHands(TrashItem);

        // Throw the item at the disposal unit
        await ThrowItem();

        // Wait a moment
        await RunTicks(10);

        // Make sure the trash is in the disposal unit
        var throwInsertComp = Comp<ThrowInsertContainerComponent>();
        var container = containerSys.GetContainer(ToServer(disposalUnit), throwInsertComp.ContainerId);
        Assert.That(container.ContainedEntities, Contains.Item(ToServer(trash)));
    }
}
