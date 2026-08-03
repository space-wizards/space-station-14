// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Xenoarchaeology.Equipment.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Inventory;
using Content.Shared.Storage.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.DeadSpace.Xenoborgs;

[TestFixture]
public sealed class XenoborgBodyCrusherTest
{
    [Test]
    public async Task CrusherKeepsOnlyBrain()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var bodySystem = server.System<SharedBodySystem>();
        var containerSystem = server.System<SharedContainerSystem>();
        var crusherSystem = server.System<ArtifactCrusherSystem>();
        var inventorySystem = server.System<InventorySystem>();

        EntityUid human = default;
        EntityUid uniform = default;
        EntityUid pocketItem = default;
        EntityUid looseItem = default;
        EntityUid crusher = default;
        EntityUid[] organs = [];

        await server.WaitAssertion(() =>
        {
            human = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            uniform = entMan.SpawnEntity("ClothingUniformJumpsuitColorGrey", testMap.GridCoords);
            pocketItem = entMan.SpawnEntity("Crowbar", testMap.GridCoords);
            looseItem = entMan.SpawnEntity("Wrench", testMap.GridCoords);
            crusher = entMan.SpawnEntity("MachineArtifactCrusherXenoborg", testMap.GridCoords);

            Assert.That(inventorySystem.TryEquip(human, uniform, "jumpsuit"), Is.True);
            Assert.That(inventorySystem.TryEquip(human, pocketItem, "pocket1"), Is.True);

            organs = bodySystem.GetBodyOrgans(human).Select(organ => organ.Id).ToArray();
            Assert.That(organs.Count(organ => entMan.HasComponent<BrainComponent>(organ)), Is.EqualTo(1));

            var crusherComp = entMan.GetComponent<ArtifactCrusherComponent>(crusher);
            var storageComp = entMan.GetComponent<EntityStorageComponent>(crusher);
            Assert.That(containerSystem.Insert(human, storageComp.Contents), Is.True);
            Assert.That(containerSystem.Insert(looseItem, storageComp.Contents), Is.True);

            crusherSystem.FinishCrushing((crusher, crusherComp, storageComp));

            Assert.That(crusherComp.OutputContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(
                crusherComp.OutputContainer.ContainedEntities.All(entMan.HasComponent<BrainComponent>),
                Is.True);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            var crusherComp = entMan.GetComponent<ArtifactCrusherComponent>(crusher);
            var brains = crusherComp.OutputContainer.ContainedEntities.ToArray();
            Assert.That(brains, Has.Length.EqualTo(1));
            Assert.That(entMan.Deleted(brains[0]), Is.False);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.Deleted(human), Is.True);
                Assert.That(entMan.Deleted(uniform), Is.True);
                Assert.That(entMan.Deleted(pocketItem), Is.True);
                Assert.That(entMan.Deleted(looseItem), Is.True);
            });

            foreach (var organ in organs.Where(organ => !entMan.HasComponent<BrainComponent>(organ)))
                Assert.That(entMan.Deleted(organ), Is.True, $"Non-brain organ {organ} survived crushing.");
        });

        await pair.CleanReturnAsync();
    }
}
