using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Changeling;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Actions.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Goobstation.Changeling;

[TestFixture]
public sealed class ChangelingArmorTest
{
    [Test]
    [TestCase("ActionToggleChitinousArmor", "ChangelingClothingOuterArmor", "ChangelingClothingHeadHelmet")]
    [TestCase("ActionToggleSpacesuit", "ChangelingClothingOuterHardsuit", "ChangelingClothingHeadHelmetHardsuit")]
    public async Task TestChangelingFullArmor(string actionProto, string outerProto, string helmetProto)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            InLobby = false,
            DummyTicker = false,
        });

        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var ticker = server.System<GameTicker>();
        var entMan = server.ResolveDependency<IEntityManager>();
        var timing = server.ResolveDependency<IGameTiming>();

        var lingSys = entMan.System<ChangelingSystem>();
        var antagSys = entMan.System<AntagSelectionSystem>();
        var mindSys = entMan.System<MindSystem>();
        var actionSys = entMan.System<ActionsSystem>();
        var invSys = entMan.System<InventorySystem>();

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

        EntityUid urist = EntityUid.Invalid;
        ChangelingComponent changeling = null;
        Entity<ActionComponent> armorAction = (EntityUid.Invalid, null);

        await server.WaitPost(() =>
        {
            // Spawn a urist
            urist = entMan.SpawnEntity("MobHuman", testMap.GridCoords);

            // Make urist a changeling
            changeling = entMan.AddComponent<ChangelingComponent>(urist);
            changeling.TotalAbsorbedEntities += 10;
            changeling.MaxChemicals = 1000;
            changeling.Chemicals = 1000;

            // Give urist chitinous armor action
            var armorActionEnt = actionSys.AddAction(urist, actionProto);
            armorAction = (armorActionEnt.Value, entMan.GetComponent<ActionComponent>(armorActionEnt.Value));
            actionSys.SetUseDelay(armorAction, null);

            // Armor up
            actionSys.PerformAction(urist, armorAction);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(invSys.TryGetSlotEntity(urist, "outerClothing", out var outerClothing), Is.True);
            Assert.That(outerClothing, Is.Not.Null);
            Assert.That(entMan.GetComponent<MetaDataComponent>(outerClothing.Value).EntityPrototype!.ID, Is.EqualTo(outerProto));

            Assert.That(invSys.TryGetSlotEntity(urist, "head", out var head));
            Assert.That(head, Is.Not.Null);
            Assert.That(entMan.GetComponent<MetaDataComponent>(head.Value).EntityPrototype!.ID, Is.EqualTo(helmetProto));
        });

        await server.WaitPost(() =>
        {
            // Armor down
            actionSys.PerformAction(urist, armorAction);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "outerClothing", out var outerClothing), Is.False);
                Assert.That(entMan.TryGetComponent<MetaDataComponent>(outerClothing, out var meta), Is.False);
                Assert.That(meta?.EntityPrototype, Is.Null);
            });

            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "head", out var head), Is.False);
                Assert.That(entMan.TryGetComponent<MetaDataComponent>(head, out var meta), Is.False);
                Assert.That(meta?.EntityPrototype, Is.Null);
            });
        });

        const string mercHelmet = "ClothingHeadHelmetMerc";

        await server.WaitPost(() =>
        {
            // Equip helmet
            var helm = entMan.SpawnEntity(mercHelmet, testMap.GridCoords);
            Assert.That(invSys.TryEquip(urist, helm, "head", force: true));

            // Try to armor up, should fail due to helmet and not equip anything
            actionSys.PerformAction(urist, armorAction);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(invSys.TryGetSlotEntity(urist, "outerClothing", out var outerClothing), Is.False);
                Assert.That(entMan.TryGetComponent<MetaDataComponent>(outerClothing, out var meta), Is.False);
                Assert.That(meta?.EntityPrototype, Is.Null);
            });

            Assert.That(invSys.TryGetSlotEntity(urist, "head", out var head));
            Assert.That(head, Is.Not.Null);
            Assert.That(entMan.GetComponent<MetaDataComponent>(head.Value).EntityPrototype!.ID, Is.EqualTo(mercHelmet));
        });

        await pair.CleanReturnAsync();
    }
}
