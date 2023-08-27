using Content.Shared.Item;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class StackTest
{
    [Test]
    public async Task StackCorrectItemSize()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        Assert.Multiple(() =>
        {
            foreach (var entity in PoolManager.GetPrototypesWithComponent<StackComponent>(server))
            {
                if (!entity.TryGetComponent<StackComponent>(out var stackComponent, compFact) ||
                    !entity.TryGetComponent<ItemComponent>(out var itemComponent, compFact))
                    continue;

                if (!protoManager.TryIndex<StackPrototype>(stackComponent.StackTypeId, out var stackProto) ||
                    stackProto.ItemSize == null)
                    continue;

                var expectedSize = stackProto.ItemSize * stackComponent.Count;
                Assert.That(itemComponent.Size, Is.EqualTo(expectedSize), $"Prototype id: {entity.ID} has an item size of {itemComponent.Size} but expected size of {expectedSize}.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
