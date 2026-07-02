#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Containers;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Storage;

public sealed class StorageTest : GameTest
{
    /// <summary>
    /// Can an item store more than itself weighs.
    /// In an ideal world this test wouldn't need to exist because sizes would be recursive.
    /// </summary>
    [Test]
    public async Task StorageSizeArbitrageTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var entMan = server.ResolveDependency<IEntityManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        var itemSys = entMan.System<SharedItemSystem>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryComp<StorageComponent>(out var storage, compFact) ||
                    storage.Whitelist != null ||
                    storage.MaxItemSize == null ||
                    !proto.TryComp<ItemComponent>(out var item, compFact))
                    continue;

                Assert.That(itemSys.GetSizePrototype(storage.MaxItemSize.Value).Weight,
                    Is.LessThanOrEqualTo(itemSys.GetSizePrototype(item.Size).Weight),
                    $"Found storage arbitrage on {proto.ID}");
            }
        });
    }

    [Test]
    public async Task TestStorageFillPrototypes()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryComp<StorageFillComponent>(out var storage, compFact))
                        continue;

                    foreach (var entry in storage.Contents)
                    {
                        Assert.That(entry.Amount, Is.GreaterThan(0), $"Specified invalid amount of {entry.Amount} for prototype {proto.ID}");
                        Assert.That(entry.SpawnProbability, Is.GreaterThan(0), $"Specified invalid probability of {entry.SpawnProbability} for prototype {proto.ID}");
                    }
                }
            });
        });
    }

    [Test]
    public async Task TestSufficientSpaceForFill()
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();
        var id = compFact.GetComponentName<StorageFillComponent>();

        var itemSys = entMan.System<SharedItemSystem>();

        var allSizes = protoMan.EnumeratePrototypes<ItemSizePrototype>().ToList();
        allSizes.Sort();

        await Assert.MultipleAsync(async () =>
        {
            foreach (var (proto, fill) in pair.GetPrototypesWithComponent<StorageFillComponent>())
            {
                if (proto.HasComponent<EntityStorageComponent>(compFact))
                    continue;

                StorageComponent? storage = null;
                ItemComponent? item = null;
                var size = 0;
                await server.WaitAssertion(() =>
                {
                    if (!proto.TryComp(out storage, compFact))
                    {
                        Assert.Fail($"Entity {proto.ID} has storage-fill without a storage component!");
                        return;
                    }

                    proto.TryComp(out item, compFact);
                    size = GetFillSize(fill, false, protoMan, compFact, itemSys);
                });

                if (storage == null)
                    continue;

                var maxSize = storage.MaxItemSize;
                if (storage.MaxItemSize == null)
                {
                    if (item?.Size == null)
                    {
                        maxSize = SharedStorageSystem.DefaultStorageMaxItemSize;
                    }
                    else
                    {
                        var curIndex = allSizes.IndexOf(protoMan.Index(item.Size));
                        var index = Math.Max(0, curIndex - 1);
                        maxSize = allSizes[index].ID;
                    }
                }

                if (maxSize == null)
                    continue;

                Assert.That(size, Is.LessThanOrEqualTo(storage.Grid.GetArea()), $"{proto.ID} storage fill is too large.");

                foreach (var entry in fill.Contents)
                {
                    if (entry.PrototypeId == null)
                        continue;

                    if (!protoMan.TryIndex<EntityPrototype>(entry.PrototypeId, out var fillItem))
                        continue;

                    ItemComponent? entryItem = null;
                    await server.WaitPost(() =>
                    {
                        fillItem.TryComp(out entryItem, compFact);
                    });

                    if (entryItem == null)
                        continue;

                    Assert.That(protoMan.Index(entryItem.Size).Weight,
                        Is.LessThanOrEqualTo(protoMan.Index(maxSize.Value).Weight),
                        $"Entity {proto.ID} has storage-fill item, {entry.PrototypeId}, that is too large");
                }
            }
        });
    }

    [Test]
    public async Task TestSufficientSpaceForEntityStorageFill()
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();
        var id = compFact.GetComponentName<StorageFillComponent>();

        var itemSys = entMan.System<SharedItemSystem>();

        foreach (var (proto, fill) in pair.GetPrototypesWithComponent<StorageFillComponent>())
        {
            if (proto.HasComponent<StorageComponent>(compFact))
                continue;

            await server.WaitAssertion(() =>
            {
                if (!proto.TryComp(out EntityStorageComponent? entStorage, compFact))
                    Assert.Fail($"Entity {proto.ID} has storage-fill without a storage component!");

                if (entStorage == null)
                    return;

                var size = GetFillSize(fill, true, protoMan, compFact, itemSys);
                Assert.That(size, Is.LessThanOrEqualTo(entStorage.Capacity),
                    $"{proto.ID} storage fill is too large.");
            });
        }
    }

    private int GetEntrySize(EntitySpawnEntry entry, bool getCount, IPrototypeManager protoMan, IComponentFactory compFact, SharedItemSystem itemSystem)
    {
        if (entry.PrototypeId == null)
            return 0;

        if (!protoMan.TryIndex<EntityPrototype>(entry.PrototypeId, out var proto))
        {
            Assert.Fail($"Unknown prototype: {entry.PrototypeId}");
            return 0;
        }

        if (getCount)
            return entry.Amount;


        if (proto.TryComp<ItemComponent>(out var item, compFact))
            return itemSystem.GetItemShape(item).GetArea() * entry.Amount;

        Assert.Fail($"Prototype is missing item comp: {entry.PrototypeId}");
        return 0;
    }

    private int GetFillSize(StorageFillComponent fill, bool getCount, IPrototypeManager protoMan, IComponentFactory compFact, SharedItemSystem itemSystem)
    {
        var totalSize = 0;
        var groups = new Dictionary<string, int>();
        foreach (var entry in fill.Contents)
        {
            var size = GetEntrySize(entry, getCount, protoMan, compFact, itemSystem);

            if (entry.GroupId == null)
                totalSize += size;
            else
                groups[entry.GroupId] = Math.Max(size, groups.GetValueOrDefault(entry.GroupId));
        }

        return totalSize + groups.Values.Sum();
    }

    /// <summary>
    /// Tests that prototypes are not using multiple container fill components at the same time.
    /// </summary>
    [Test]
    public async Task NoMultipleContainerFillsTest()
    {
        var pair = Pair;
        var compFact = pair.Server.ResolveDependency<IComponentFactory>();

        Assert.Multiple(() =>
        {
            foreach (var (proto, fill) in pair.GetPrototypesWithComponent<EntityTableContainerFillComponent>())
            {
                Assert.That(!proto.HasComp<StorageFillComponent>(compFact), $"Prototype {proto.ID} has both {nameof(EntityTableContainerFillComponent)} and {nameof(StorageFillComponent)}.");
                Assert.That(!proto.HasComp<ContainerFillComponent>(compFact), $"Prototype {proto.ID} has both {nameof(EntityTableContainerFillComponent)} and {nameof(ContainerFillComponent)}.");
            }

            foreach (var (proto, fill) in pair.GetPrototypesWithComponent<ContainerFillComponent>())
            {
                Assert.That(!proto.HasComp<StorageFillComponent>(compFact), $"Prototype {proto.ID} has both {nameof(ContainerFillComponent)} and {nameof(StorageFillComponent)}.");
            }
        });
    }
}
