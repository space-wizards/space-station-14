// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Collections.Generic;
using System.Linq;
using Content.Server.Stunnable;
using Content.Shared.Configurable;
using Content.Shared.DeadSpace.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.DeadSpace;

[TestFixture]
[NonParallelizable]
public sealed class MultiClothingTests
{
    private const string TargetPrototype = "MultiClothingTestTarget";
    private const string BlockingTargetPrototype = "MultiClothingTestBlockingTarget";
    private const string OriginalJumpsuitPrototype = "MultiClothingTestOriginalJumpsuit";
    private const string BlockingOriginalJumpsuitPrototype = "MultiClothingTestBlockingOriginalJumpsuit";
    private const string AuxiliaryJumpsuitPrototype = "MultiClothingTestAuxiliaryJumpsuit";
    private const string PocketItemPrototype = "MultiClothingTestPocketItem";
    private const string SuitStoragePrototype = "MultiClothingTestSuitStorage";
    private const string IdPrototype = "MultiClothingTestId";
    private const string ReplacingHostPrototype = "MultiClothingTestReplacingHost";
    private const string OverlappingHostPrototype = "MultiClothingTestOverlappingHost";
    private const string BlockingAuxiliaryPrototype = "MultiClothingTestBlockingAuxiliary";
    private const string FailingHostPrototype = "MultiClothingTestFailingHost";
    private const string CyclicHostPrototype = "MultiClothingTestCyclicHost";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: MultiClothingTestTarget
  components:
  - type: Inventory
  - type: ContainerContainer
  - type: MobState

- type: entity
  id: MultiClothingTestBlockingTarget
  parent: MultiClothingTestTarget
  components:
  - type: Configuration
    qualityNeeded: Screwing

- type: entity
  id: MultiClothingTestOriginalJumpsuit
  components:
  - type: Clothing
    slots: [INNERCLOTHING]

- type: entity
  id: MultiClothingTestBlockingOriginalJumpsuit
  parent: MultiClothingTestOriginalJumpsuit
  components:
  - type: Tool
    qualities:
    - Screwing

- type: entity
  id: MultiClothingTestAuxiliaryJumpsuit
  components:
  - type: Clothing
    slots: [INNERCLOTHING]

- type: entity
  id: MultiClothingTestPocketItem
  components:
  - type: Item
    size: Tiny

- type: entity
  id: MultiClothingTestSuitStorage
  components:
  - type: Clothing
    slots: [SUITSTORAGE]

- type: entity
  id: MultiClothingTestId
  components:
  - type: Clothing
    slots: [IDCARD]

- type: entity
  id: MultiClothingTestReplacingHost
  components:
  - type: Clothing
    slots: [BACK]
  - type: MultiClothing
    force: true
    equipment:
      jumpsuit: MultiClothingTestAuxiliaryJumpsuit
  - type: ContainerContainer

- type: entity
  id: MultiClothingTestOverlappingHost
  components:
  - type: Clothing
    slots: [BELT]
  - type: MultiClothing
    force: true
    equipment:
      jumpsuit: MultiClothingTestAuxiliaryJumpsuit
  - type: ContainerContainer

- type: entity
  id: MultiClothingTestBlockingAuxiliary
  components:
  - type: Clothing
    slots: [HEAD]
  - type: Tool
    qualities:
    - Screwing

- type: entity
  id: MultiClothingTestFailingHost
  components:
  - type: Clothing
    slots: [BACK]
  - type: MultiClothing
    equipment:
      head: MultiClothingTestBlockingAuxiliary
  - type: ContainerContainer

- type: entity
  id: MultiClothingTestCyclicHost
  components:
  - type: Item
    size: Tiny
  - type: MultiClothing
    force: true
    equipment:
      jumpsuit: MultiClothingTestAuxiliaryJumpsuit
  - type: ContainerContainer
";

    [Test]
    public async Task ForcedJumpsuitReplacementPreservesDependentSlotsAndRestoresWhileStunned()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var containers = server.System<SharedContainerSystem>();
        var stun = server.System<StunSystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid target = default;
        EntityUid host = default;
        EntityUid originalJumpsuit = default;
        EntityUid pocket1 = default;
        EntityUid pocket2 = default;
        EntityUid suitStorage = default;
        EntityUid id = default;
        EntityUid auxiliaryJumpsuit = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            target = entityManager.SpawnEntity(TargetPrototype, coordinates);
            host = entityManager.SpawnEntity(ReplacingHostPrototype, coordinates);
            originalJumpsuit = entityManager.SpawnEntity(OriginalJumpsuitPrototype, coordinates);
            pocket1 = entityManager.SpawnEntity(PocketItemPrototype, coordinates);
            pocket2 = entityManager.SpawnEntity(PocketItemPrototype, coordinates);
            suitStorage = entityManager.SpawnEntity(SuitStoragePrototype, coordinates);
            id = entityManager.SpawnEntity(IdPrototype, coordinates);

            Assert.That(inventory.TryEquip(target, originalJumpsuit, "jumpsuit", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, pocket1, "pocket1", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, pocket2, "pocket2", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, suitStorage, "suitstorage", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, id, "id", force: true), Is.True);

            Assert.That(inventory.TryEquip(target, host, "back", force: true), Is.True);

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems.Keys, Is.EquivalentTo(new[] { "jumpsuit" }));
            Assert.That(component.ForcedOffItems.Keys, Is.EquivalentTo(new[] { "jumpsuit" }));
            auxiliaryJumpsuit = component.SpawnedItems["jumpsuit"];
            Assert.That(component.ForcedOffItems["jumpsuit"], Is.EqualTo(originalJumpsuit));

            AssertSlot(inventory, target, "back", host);
            AssertSlot(inventory, target, "jumpsuit", auxiliaryJumpsuit);
            AssertSlot(inventory, target, "pocket1", pocket1);
            AssertSlot(inventory, target, "pocket2", pocket2);
            AssertSlot(inventory, target, "suitstorage", suitStorage);
            AssertSlot(inventory, target, "id", id);

            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.ContainedEntities, Is.EquivalentTo(new[] { originalJumpsuit }));

            Assert.That(stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(30)), Is.True);
            Assert.That(entityManager.HasComponent<StunnedComponent>(target), Is.True);
            Assert.That(inventory.TryUnequip(target, "back", force: true), Is.True);

            AssertSlotEmpty(inventory, target, "back");
            AssertSlot(inventory, target, "jumpsuit", auxiliaryJumpsuit);
            Assert.That(component.SpawnedItems.Keys, Is.EquivalentTo(new[] { "jumpsuit" }));
            Assert.That(component.ForcedOffItems.Keys, Is.EquivalentTo(new[] { "jumpsuit" }));
            Assert.That(privateContainer.ContainedEntities, Is.EquivalentTo(new[] { originalJumpsuit }));
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entityManager.HasComponent<StunnedComponent>(target), Is.True);
            AssertSlotEmpty(inventory, target, "back");
            AssertSlot(inventory, target, "jumpsuit", originalJumpsuit);
            AssertSlot(inventory, target, "pocket1", pocket1);
            AssertSlot(inventory, target, "pocket2", pocket2);
            AssertSlot(inventory, target, "suitstorage", suitStorage);
            AssertSlot(inventory, target, "id", id);

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.Empty);
            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.ContainedEntities, Is.EquivalentTo(new[] { auxiliaryJumpsuit }));

            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AuxiliaryInsertionFailureRollsBackHostOnQueuedTickWithoutFloorLeak()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var containers = server.System<SharedContainerSystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid target = default;
        EntityUid host = default;
        EntityUid failedAuxiliary = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            target = entityManager.SpawnEntity(BlockingTargetPrototype, coordinates);
            host = entityManager.SpawnEntity(FailingHostPrototype, coordinates);

            Assert.That(inventory.TryEquip(target, host, "back", force: true), Is.True);

            AssertSlot(inventory, target, "back", host);
            AssertSlotEmpty(inventory, target, "head");

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.Empty);
            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.ContainedEntities, Has.Count.EqualTo(1));
            failedAuxiliary = privateContainer.ContainedEntities.Single();
            Assert.That(entityManager.GetComponent<MetaDataComponent>(failedAuxiliary).EntityPrototype?.ID,
                Is.EqualTo(BlockingAuxiliaryPrototype));
            Assert.That(containers.IsEntityInContainer(failedAuxiliary), Is.True);
            Assert.That(entityManager.GetComponent<TransformComponent>(failedAuxiliary).ParentUid, Is.EqualTo(host));
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            AssertSlotEmpty(inventory, target, "back");
            AssertSlotEmpty(inventory, target, "head");

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.Empty);
            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.ContainedEntities, Is.EquivalentTo(new[] { failedAuxiliary }));
            Assert.That(containers.IsEntityInContainer(failedAuxiliary), Is.True);
            Assert.That(containers.IsEntityInContainer(host), Is.False);
            Assert.That(entityManager.GetComponent<TransformComponent>(failedAuxiliary).ParentUid, Is.EqualTo(host));
            Assert.That(entityManager.GetComponent<TransformComponent>(host).MapID, Is.EqualTo(testMap.MapId));

            var auxiliaryCount = entityManager.EntityQuery<MetaDataComponent>()
                .Count(meta => !meta.Deleted && meta.EntityPrototype?.ID == BlockingAuxiliaryPrototype);
            Assert.That(auxiliaryCount, Is.EqualTo(1));

            entityManager.RemoveComponent<MultiClothingComponent>(host);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entityManager.Deleted(failedAuxiliary), Is.True);
            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.ContainedEntities, Is.Empty);

            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DependentItemAddedUnderGeneratedParentDropsWhenHostIsRemoved()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var containers = server.System<SharedContainerSystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid target = default;
        EntityUid host = default;
        EntityUid pocket = default;
        EntityUid auxiliaryJumpsuit = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            target = entityManager.SpawnEntity(TargetPrototype, coordinates);
            host = entityManager.SpawnEntity(ReplacingHostPrototype, coordinates);
            pocket = entityManager.SpawnEntity(PocketItemPrototype, coordinates);

            Assert.That(inventory.TryEquip(target, host, "back", force: true), Is.True);
            auxiliaryJumpsuit = entityManager.GetComponent<MultiClothingComponent>(host).SpawnedItems["jumpsuit"];
            Assert.That(inventory.TryEquip(target, pocket, "pocket1", force: true), Is.True);

            AssertSlot(inventory, target, "jumpsuit", auxiliaryJumpsuit);
            AssertSlot(inventory, target, "pocket1", pocket);
            Assert.That(inventory.TryUnequip(target, "back", force: true), Is.True);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            AssertSlotEmpty(inventory, target, "back");
            AssertSlotEmpty(inventory, target, "jumpsuit");
            AssertSlotEmpty(inventory, target, "pocket1");

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.Empty);
            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.ContainedEntities, Is.EquivalentTo(new[] { auxiliaryJumpsuit }));
            Assert.That(containers.IsEntityInContainer(pocket), Is.False);
            Assert.That(entityManager.GetComponent<TransformComponent>(pocket).MapID, Is.EqualTo(testMap.MapId));

            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task OverlappingForceBundleRollsBackWithoutDeletingFirstAuxiliary()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid target = default;
        EntityUid firstHost = default;
        EntityUid secondHost = default;
        EntityUid firstAuxiliary = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            target = entityManager.SpawnEntity(TargetPrototype, coordinates);
            firstHost = entityManager.SpawnEntity(ReplacingHostPrototype, coordinates);
            secondHost = entityManager.SpawnEntity(OverlappingHostPrototype, coordinates);

            Assert.That(inventory.TryEquip(target, firstHost, "back", force: true), Is.True);
            var firstComponent = entityManager.GetComponent<MultiClothingComponent>(firstHost);
            firstAuxiliary = firstComponent.SpawnedItems["jumpsuit"];

            Assert.That(inventory.TryEquip(target, secondHost, "belt", force: true), Is.True);
            AssertSlot(inventory, target, "back", firstHost);
            AssertSlot(inventory, target, "belt", secondHost);
            AssertSlot(inventory, target, "jumpsuit", firstAuxiliary);
            Assert.That(firstComponent.SpawnedItems,
                Is.EquivalentTo(new Dictionary<string, EntityUid> { ["jumpsuit"] = firstAuxiliary }));
            Assert.That(firstComponent.ForcedOffItems, Is.Empty);

            var secondComponent = entityManager.GetComponent<MultiClothingComponent>(secondHost);
            Assert.That(secondComponent.SpawnedItems, Is.Empty);
            Assert.That(secondComponent.ForcedOffItems, Is.Empty);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            AssertSlot(inventory, target, "back", firstHost);
            AssertSlotEmpty(inventory, target, "belt");
            AssertSlot(inventory, target, "jumpsuit", firstAuxiliary);
            Assert.That(entityManager.Deleted(firstAuxiliary), Is.False);

            var firstComponent = entityManager.GetComponent<MultiClothingComponent>(firstHost);
            Assert.That(firstComponent.SpawnedItems,
                Is.EquivalentTo(new Dictionary<string, EntityUid> { ["jumpsuit"] = firstAuxiliary }));
            Assert.That(firstComponent.ForcedOffItems, Is.Empty);

            var secondComponent = entityManager.GetComponent<MultiClothingComponent>(secondHost);
            Assert.That(secondComponent.SpawnedItems, Is.Empty);
            Assert.That(secondComponent.ForcedOffItems, Is.Empty);

            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ReequipBeforeDeferredCleanupRestoresOnlyOriginalTarget()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var containers = server.System<SharedContainerSystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid targetA = default;
        EntityUid targetB = default;
        EntityUid host = default;
        EntityUid originalJumpsuit = default;
        EntityUid pocket = default;
        EntityUid auxiliaryJumpsuit = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            targetA = entityManager.SpawnEntity(TargetPrototype, coordinates);
            targetB = entityManager.SpawnEntity(TargetPrototype, coordinates);
            host = entityManager.SpawnEntity(ReplacingHostPrototype, coordinates);
            originalJumpsuit = entityManager.SpawnEntity(OriginalJumpsuitPrototype, coordinates);
            pocket = entityManager.SpawnEntity(PocketItemPrototype, coordinates);

            Assert.That(inventory.TryEquip(targetA, originalJumpsuit, "jumpsuit", force: true), Is.True);
            Assert.That(inventory.TryEquip(targetA, pocket, "pocket1", force: true), Is.True);
            Assert.That(inventory.TryEquip(targetA, host, "back", force: true), Is.True);
            auxiliaryJumpsuit = entityManager.GetComponent<MultiClothingComponent>(host).SpawnedItems["jumpsuit"];

            Assert.That(inventory.TryUnequip(targetA, "back", force: true), Is.True);
            Assert.That(inventory.TryEquip(targetB, host, "back", force: true), Is.True);

            AssertSlotEmpty(inventory, targetA, "back");
            AssertSlot(inventory, targetA, "jumpsuit", auxiliaryJumpsuit);
            AssertSlot(inventory, targetA, "pocket1", pocket);
            AssertSlot(inventory, targetB, "back", host);
            AssertSlotEmpty(inventory, targetB, "jumpsuit");
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            AssertSlotEmpty(inventory, targetA, "back");
            AssertSlot(inventory, targetA, "jumpsuit", originalJumpsuit);
            AssertSlot(inventory, targetA, "pocket1", pocket);
            AssertSlotEmpty(inventory, targetB, "back");
            AssertSlotEmpty(inventory, targetB, "jumpsuit");
            Assert.That(containers.IsEntityInContainer(host), Is.False);

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.Empty);
            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.ContainedEntities, Is.EquivalentTo(new[] { auxiliaryJumpsuit }));

            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FailedOriginalRestoreRemainsTrackedAndRetriesWithoutFloorLeak()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var containers = server.System<SharedContainerSystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid target = default;
        EntityUid host = default;
        EntityUid originalJumpsuit = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            target = entityManager.SpawnEntity(TargetPrototype, coordinates);
            host = entityManager.SpawnEntity(ReplacingHostPrototype, coordinates);
            originalJumpsuit = entityManager.SpawnEntity(BlockingOriginalJumpsuitPrototype, coordinates);

            Assert.That(inventory.TryEquip(target, originalJumpsuit, "jumpsuit", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, host, "back", force: true), Is.True);

            var configuration = entityManager.AddComponent<ConfigurationComponent>(target);
            configuration.QualityNeeded = "Screwing";
            Assert.That(inventory.TryUnequip(target, "back", force: true), Is.True);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            AssertSlotEmpty(inventory, target, "back");
            AssertSlotEmpty(inventory, target, "jumpsuit");

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.EquivalentTo(
                new Dictionary<string, EntityUid> { ["jumpsuit"] = originalJumpsuit }));
            Assert.That(containers.TryGetContainer(host, MultiClothingSystem.ContainerId, out var privateContainer),
                Is.True);
            Assert.That(privateContainer!.Contains(originalJumpsuit), Is.True);
            Assert.That(entityManager.GetComponent<TransformComponent>(originalJumpsuit).ParentUid, Is.EqualTo(host));

            entityManager.RemoveComponent<ConfigurationComponent>(target);
            Assert.That(inventory.TryEquip(target, host, "back", force: true), Is.True);
        });

        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            AssertSlotEmpty(inventory, target, "back");
            AssertSlot(inventory, target, "jumpsuit", originalJumpsuit);

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.Empty);

            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TargetSlotClosureContainingHostRollsBackWithoutReentrantUnequip()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid target = default;
        EntityUid host = default;
        EntityUid jumpsuit = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            target = entityManager.SpawnEntity(TargetPrototype, coordinates);
            host = entityManager.SpawnEntity(CyclicHostPrototype, coordinates);
            jumpsuit = entityManager.SpawnEntity(OriginalJumpsuitPrototype, coordinates);

            Assert.That(inventory.TryEquip(target, jumpsuit, "jumpsuit", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, host, "pocket1", force: true), Is.True);

            AssertSlot(inventory, target, "jumpsuit", jumpsuit);
            AssertSlot(inventory, target, "pocket1", host);

            var component = entityManager.GetComponent<MultiClothingComponent>(host);
            Assert.That(component.SpawnedItems, Is.Empty);
            Assert.That(component.ForcedOffItems, Is.Empty);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            AssertSlot(inventory, target, "jumpsuit", jumpsuit);
            AssertSlotEmpty(inventory, target, "pocket1");
            Assert.That(entityManager.GetComponent<TransformComponent>(host).MapID, Is.EqualTo(testMap.MapId));

            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeletingEquippedHostReleasesForcedOffItemBeforeContainerRecursion()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var inventory = server.System<InventorySystem>();
        var map = server.System<SharedMapSystem>();

        EntityUid target = default;
        EntityUid host = default;
        EntityUid originalJumpsuit = default;
        EntityUid pocket1 = default;
        EntityUid pocket2 = default;
        EntityUid suitStorage = default;
        EntityUid id = default;
        EntityUid auxiliaryJumpsuit = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            target = entityManager.SpawnEntity(TargetPrototype, coordinates);
            host = entityManager.SpawnEntity(ReplacingHostPrototype, coordinates);
            originalJumpsuit = entityManager.SpawnEntity(OriginalJumpsuitPrototype, coordinates);
            pocket1 = entityManager.SpawnEntity(PocketItemPrototype, coordinates);
            pocket2 = entityManager.SpawnEntity(PocketItemPrototype, coordinates);
            suitStorage = entityManager.SpawnEntity(SuitStoragePrototype, coordinates);
            id = entityManager.SpawnEntity(IdPrototype, coordinates);

            Assert.That(inventory.TryEquip(target, originalJumpsuit, "jumpsuit", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, pocket1, "pocket1", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, pocket2, "pocket2", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, suitStorage, "suitstorage", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, id, "id", force: true), Is.True);
            Assert.That(inventory.TryEquip(target, host, "back", force: true), Is.True);

            auxiliaryJumpsuit = entityManager.GetComponent<MultiClothingComponent>(host).SpawnedItems["jumpsuit"];
            AssertSlot(inventory, target, "jumpsuit", auxiliaryJumpsuit);

            entityManager.DeleteEntity(host);

            Assert.That(entityManager.Deleted(host), Is.True);
            Assert.That(entityManager.Deleted(originalJumpsuit), Is.False);
            AssertSlotEmpty(inventory, target, "back");
            AssertSlot(inventory, target, "jumpsuit", originalJumpsuit);
            AssertSlot(inventory, target, "pocket1", pocket1);
            AssertSlot(inventory, target, "pocket2", pocket2);
            AssertSlot(inventory, target, "suitstorage", suitStorage);
            AssertSlot(inventory, target, "id", id);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entityManager.Deleted(auxiliaryJumpsuit), Is.True);
            map.DeleteMap(testMap.MapId);
        });

        await pair.CleanReturnAsync();
    }

    private static void AssertSlot(
        InventorySystem inventory,
        EntityUid target,
        string slot,
        EntityUid expected)
    {
        Assert.That(inventory.TryGetSlotEntity(target, slot, out var actual), Is.True);
        Assert.That(actual, Is.EqualTo(expected));
    }

    private static void AssertSlotEmpty(InventorySystem inventory, EntityUid target, string slot)
    {
        Assert.That(inventory.TryGetSlotEntity(target, slot, out _), Is.False);
    }
}
