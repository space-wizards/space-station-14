// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

#nullable enable

using System.Linq;
using Content.Server.DeadSpace.RoundEnd;
using Content.Shared.Gibbing;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Client.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.DeadSpace.RoundEnd;

[TestFixture]
[NonParallelizable]
public sealed class RoundEndDollStateTests
{
    private const string BodyPrototype = "MobHuman";
    private const string UniformPrototype = "ClothingUniformJumpsuitColorGrey";
    private const string SecondUniformPrototype = "ClothingUniformJumpsuitColorBlue";
    private const string NonHumanoidPrototype = "RoundEndDollTestNonHumanoid";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: RoundEndDollTestNonHumanoid
  name: round end doll test character
  components:
  - type: MindContainer
  - type: Body
";

    [Test]
    public async Task EquipUnequipAndCorpseStripUpdateOneSlot()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var inventory = server.System<InventorySystem>();
            var dollState = server.System<RoundEndDollStateSystem>();
            var (body, mind) = SpawnPlayerMind(server, BodyPrototype);

            if (inventory.TryUnequip(body, "jumpsuit", out var oldUniform, silent: true, force: true) &&
                oldUniform != null)
            {
                entMan.DeleteEntity(oldUniform.Value);
            }

            var uniform = entMan.SpawnEntity(UniformPrototype, MapCoordinates.Nullspace);
            Assert.That(inventory.TryEquip(body, uniform, "jumpsuit", true, true), Is.True);
            Assert.That(
                dollState.GetDollData(mind)!.Equipment.Single(entry => entry.Slot == "jumpsuit").Prototype.Id,
                Is.EqualTo(UniformPrototype));

            server.System<SharedMindSystem>().TransferTo(mind, null);
            Assert.That(inventory.TryUnequip(body, "jumpsuit", silent: true, force: true), Is.True);
            Assert.That(dollState.GetDollData(mind)!.Equipment.Any(entry => entry.Slot == "jumpsuit"), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task GibKeepsEquipmentInEntityFreeData()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        EntityUid body = default;
        EntityUid mind = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var inventory = server.System<InventorySystem>();
            var dollState = server.System<RoundEndDollStateSystem>();
            (body, mind) = SpawnPlayerMind(server, BodyPrototype, map.MapCoords);

            ClearSlot(entMan, inventory, body, "jumpsuit");
            var uniform = entMan.SpawnEntity(UniformPrototype, map.MapCoords);
            Assert.That(inventory.TryEquip(body, uniform, "jumpsuit", true, true), Is.True);
            Assert.That(
                dollState.GetDollData(mind)!.Equipment.Single(entry => entry.Slot == "jumpsuit").Prototype.Id,
                Is.EqualTo(UniformPrototype));

            server.System<GibbingSystem>().Gib(body);

            var after = dollState.GetDollData(mind);
            Assert.Multiple(() =>
            {
                Assert.That(after, Is.Not.Null);
                Assert.That(after!.BodyPrototype?.Id, Is.EqualTo(BodyPrototype));
                Assert.That(
                    after.Equipment.Single(entry => entry.Slot == "jumpsuit").Prototype.Id,
                    Is.EqualTo(UniformPrototype));
            });
        });

        await pair.RunTicksSync(1);
        await server.WaitAssertion(() =>
        {
            var after = server.System<RoundEndDollStateSystem>().GetDollData(mind);
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.EntityExists(body), Is.False);
                Assert.That(after, Is.Not.Null);
                Assert.That(
                    after!.Equipment.Single(entry => entry.Slot == "jumpsuit").Prototype.Id,
                    Is.EqualTo(UniformPrototype));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BodyTakeoverKeepsBothMindMappingsConsistent()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var inventory = server.System<InventorySystem>();
            var minds = server.System<SharedMindSystem>();
            var dollState = server.System<RoundEndDollStateSystem>();
            var (firstBody, firstMind) = SpawnPlayerMind(server, BodyPrototype);
            var (secondBody, secondMind) = SpawnPlayerMind(server, BodyPrototype);

            // This is the same ownership order used by mind-swap: free the target, move the first
            // mind into it, then move the second mind into the first body.
            minds.TransferTo(secondMind, null, createGhost: false);
            minds.TransferTo(firstMind, secondBody);
            minds.TransferTo(secondMind, firstBody);

            ClearSlot(entMan, inventory, firstBody, "jumpsuit");
            ClearSlot(entMan, inventory, secondBody, "jumpsuit");

            var firstUniform = entMan.SpawnEntity(UniformPrototype, MapCoordinates.Nullspace);
            var secondUniform = entMan.SpawnEntity(SecondUniformPrototype, MapCoordinates.Nullspace);
            Assert.That(inventory.TryEquip(secondBody, firstUniform, "jumpsuit", true, true), Is.True);
            Assert.That(inventory.TryEquip(firstBody, secondUniform, "jumpsuit", true, true), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(
                    dollState.GetDollData(firstMind)!.Equipment.Single(entry => entry.Slot == "jumpsuit").Prototype.Id,
                    Is.EqualTo(UniformPrototype));
                Assert.That(
                    dollState.GetDollData(secondMind)!.Equipment.Single(entry => entry.Slot == "jumpsuit").Prototype.Id,
                    Is.EqualTo(SecondUniformPrototype));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RebodyUpdatesNonHumanoidButObserverAndGhostRoleAreIgnored()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var minds = server.System<SharedMindSystem>();
            var roles = server.System<SharedRoleSystem>();
            var dollState = server.System<RoundEndDollStateSystem>();
            var (body, mind) = SpawnPlayerMind(server, BodyPrototype);

            var replacement = entMan.SpawnEntity(NonHumanoidPrototype, MapCoordinates.Nullspace);
            server.System<MetaDataSystem>().SetEntityName(replacement, entMan.GetComponent<MetaDataComponent>(body).EntityName);
            minds.TransferTo(mind, replacement);
            var replacementData = dollState.GetDollData(mind);
            Assert.Multiple(() =>
            {
                Assert.That(replacementData!.BodyPrototype?.Id, Is.EqualTo(NonHumanoidPrototype));
                Assert.That(replacementData.Humanoid, Is.Null);
            });

            var observer = entMan.SpawnEntity("MobObserver", MapCoordinates.Nullspace);
            minds.TransferTo(mind, observer);
            Assert.That(dollState.GetDollData(mind)!.BodyPrototype?.Id, Is.EqualTo(NonHumanoidPrototype));

            roles.MindAddRole(mind, "MindRoleGhostRoleNeutral");
            var ghostRoleBody = entMan.SpawnEntity(BodyPrototype, MapCoordinates.Nullspace);
            minds.TransferTo(mind, ghostRoleBody);
            Assert.That(dollState.GetDollData(mind)!.BodyPrototype?.Id, Is.EqualTo(NonHumanoidPrototype));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DisconnectReconnectKeepsDollData()
    {
        var settings = new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false,
        };
        await using var pair = await PoolManager.GetServerClient(settings);
        var server = pair.Server;
        var client = pair.Client;
        var mind = pair.PlayerData!.Mind!.Value;
        var dollState = server.System<RoundEndDollStateSystem>();

        await server.WaitAssertion(() => Assert.That(dollState.GetDollData(mind), Is.Not.Null));

        var console = client.ResolveDependency<IClientConsoleHost>();
        var network = client.ResolveDependency<IClientNetManager>();
        await client.WaitPost(() => console.ExecuteCommand("disconnect"));
        await pair.RunTicksSync(5);
        await server.WaitAssertion(() => Assert.That(dollState.GetDollData(mind), Is.Not.Null));

        client.SetConnectTarget(server);
        await client.WaitPost(() => network.ClientConnect(null!, 0, null!));
        await pair.RunTicksSync(10);
        await server.WaitAssertion(() => Assert.That(dollState.GetDollData(mind), Is.Not.Null));

        await pair.CleanReturnAsync();
    }

    private static (EntityUid Body, EntityUid Mind) SpawnPlayerMind(
        RobustIntegrationTest.ServerIntegrationInstance server,
        EntProtoId prototype,
        MapCoordinates? coordinates = null)
    {
        var body = server.EntMan.SpawnEntity(prototype, coordinates ?? MapCoordinates.Nullspace);
        var minds = server.System<SharedMindSystem>();
        var mind = minds.CreateMind(null);
#pragma warning disable RA0002
        mind.Comp.OriginalOwnerUserId = new NetUserId(Guid.NewGuid());
#pragma warning restore RA0002
        minds.TransferTo(mind.Owner, body);
        return (body, mind.Owner);
    }

    private static void ClearSlot(
        IEntityManager entMan,
        InventorySystem inventory,
        EntityUid body,
        string slot)
    {
        if (inventory.TryUnequip(body, slot, out var item, silent: true, force: true) && item != null)
            entMan.DeleteEntity(item.Value);
    }
}
