using Content.IntegrationTests.Fixtures;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Plasmaman;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.IntegrationTests.Tests._EE.Plasmaman;

[TestFixture]
[TestOf(typeof(PlasmamanOxygenIgnitionSystem))]
public sealed class PlasmamanOxygenIgnitionTest : GameTest
{
    [Test]
    public async Task PlasmamanIgnitesInOxygen()
    {
        var mapLoader = Server.EntMan.System<MapLoaderSystem>();
        var mapSystem = Server.EntMan.System<SharedMapSystem>();
        var testMapName = new ResPath("Maps/Test/Breathing/3by3-20oxy-80nit.yml");
        EntityUid? grid = null;
        EntityUid mob = default;

        await Server.WaitPost(() =>
        {
            mapSystem.CreateMap(out var mapId);
            Assert.That(mapLoader.TryLoadGrid(mapId, testMapName, out var gridEnt));
            grid = gridEnt!.Value.Owner;
        });

        Assert.That(grid, Is.Not.Null, $"Test blueprint {testMapName} not found.");

        await Server.WaitAssertion(() =>
        {
            var atmosphere = Server.EntMan.System<AtmosphereSystem>();
            _ = Server.EntMan.System<PlasmamanOxygenIgnitionSystem>();
            var coordinates = new EntityCoordinates(grid.Value, new Vector2(0.5f, 0.5f));

            mob = Server.EntMan.SpawnEntity("MobPlasmaman", coordinates);
            Assert.That(Server.EntMan.HasComponent<PlasmamanOxygenIgnitionComponent>(mob), Is.True);
            Assert.That(Server.EntMan.HasComponent<FlammableComponent>(mob), Is.True);
            Assert.That(Server.EntMan.HasComponent<MobStateComponent>(mob), Is.True);

            var mixture = atmosphere.GetContainingMixture(mob, excite: true);
            Assert.That(mixture, Is.Not.Null);
            Assert.That(mixture!.GetMoles(Gas.Oxygen), Is.GreaterThan(0.5f));
        });

        await Pair.RunSeconds(1.2f);

        await Server.WaitAssertion(() =>
        {
            var flammable = Server.EntMan.GetComponent<FlammableComponent>(mob);
            var atmosphere = Server.EntMan.System<AtmosphereSystem>();
            var mixture = atmosphere.GetContainingMixture(mob, excite: true);

            Assert.That(mixture, Is.Not.Null);
            Assert.That(mixture!.GetMoles(Gas.Oxygen), Is.GreaterThan(0.5f));

            Assert.That(flammable.FireStacks, Is.GreaterThan(0f));
            Assert.That(flammable.OnFire, Is.True);
        });
    }

    [Test]
    public async Task PlasmamanEnviroGearPreventsOxygenIgnition()
    {
        var mapLoader = Server.EntMan.System<MapLoaderSystem>();
        var mapSystem = Server.EntMan.System<SharedMapSystem>();
        var testMapName = new ResPath("Maps/Test/Breathing/3by3-20oxy-80nit.yml");
        EntityUid? grid = null;
        EntityUid mob = default;

        await Server.WaitPost(() =>
        {
            mapSystem.CreateMap(out var mapId);
            Assert.That(mapLoader.TryLoadGrid(mapId, testMapName, out var gridEnt));
            grid = gridEnt!.Value.Owner;
        });

        Assert.That(grid, Is.Not.Null, $"Test blueprint {testMapName} not found.");

        await Server.WaitAssertion(() =>
        {
            var atmosphere = Server.EntMan.System<AtmosphereSystem>();
            var inventory = Server.EntMan.System<InventorySystem>();
            _ = Server.EntMan.System<PlasmamanOxygenIgnitionSystem>();
            var coordinates = new EntityCoordinates(grid.Value, new Vector2(0.5f, 0.5f));

            mob = Server.EntMan.SpawnEntity("MobPlasmaman", coordinates);
            var suit = Server.EntMan.SpawnEntity("ClothingUniformEnvirosuit", coordinates);
            var helmet = Server.EntMan.SpawnEntity("ClothingHeadEnvirohelm", coordinates);

            Assert.That(inventory.TryEquip(mob, suit, "jumpsuit", true, true), Is.True);
            Assert.That(inventory.TryEquip(mob, helmet, "head", true, true), Is.True);
            Assert.That(Server.EntMan.HasComponent<PlasmamanOxygenIgnitionComponent>(mob), Is.True);
            Assert.That(Server.EntMan.HasComponent<FlammableComponent>(mob), Is.True);
            Assert.That(Server.EntMan.HasComponent<MobStateComponent>(mob), Is.True);

            var mixture = atmosphere.GetContainingMixture(mob, excite: true);
            Assert.That(mixture, Is.Not.Null);
            Assert.That(mixture!.GetMoles(Gas.Oxygen), Is.GreaterThan(0.5f));
        });

        await Pair.RunSeconds(1.2f);

        await Server.WaitAssertion(() =>
        {
            var flammable = Server.EntMan.GetComponent<FlammableComponent>(mob);
            var atmosphere = Server.EntMan.System<AtmosphereSystem>();
            var mixture = atmosphere.GetContainingMixture(mob, excite: true);

            Assert.That(mixture, Is.Not.Null);
            Assert.That(mixture!.GetMoles(Gas.Oxygen), Is.GreaterThan(0.5f));

            Assert.That(flammable.FireStacks, Is.EqualTo(0f));
            Assert.That(flammable.OnFire, Is.False);
        });
    }

    [Test]
    public async Task PlasmamanHardsuitPreventsOxygenIgnition()
    {
        var mapLoader = Server.EntMan.System<MapLoaderSystem>();
        var mapSystem = Server.EntMan.System<SharedMapSystem>();
        var testMapName = new ResPath("Maps/Test/Breathing/3by3-20oxy-80nit.yml");
        EntityUid? grid = null;
        EntityUid mob = default;

        await Server.WaitPost(() =>
        {
            mapSystem.CreateMap(out var mapId);
            Assert.That(mapLoader.TryLoadGrid(mapId, testMapName, out var gridEnt));
            grid = gridEnt!.Value.Owner;
        });

        Assert.That(grid, Is.Not.Null, $"Test blueprint {testMapName} not found.");

        await Server.WaitAssertion(() =>
        {
            var inventory = Server.EntMan.System<InventorySystem>();
            _ = Server.EntMan.System<PlasmamanOxygenIgnitionSystem>();
            var coordinates = new EntityCoordinates(grid.Value, new Vector2(0.5f, 0.5f));

            mob = Server.EntMan.SpawnEntity("MobPlasmaman", coordinates);
            var hardsuit = Server.EntMan.SpawnEntity("ClothingOuterHardsuitAtmos", coordinates);
            var hardsuitHelmet = Server.EntMan.SpawnEntity("ClothingHeadHelmetHardsuitAtmos", coordinates);

            Assert.That(inventory.TryEquip(mob, hardsuit, "outerClothing", true, true), Is.True);
            Assert.That(inventory.TryEquip(mob, hardsuitHelmet, "head", true, true), Is.True);
        });

        await Pair.RunSeconds(1.2f);

        await Server.WaitAssertion(() =>
        {
            var flammable = Server.EntMan.GetComponent<FlammableComponent>(mob);
            Assert.That(flammable.FireStacks, Is.EqualTo(0f), "Hardsuit + EVA helmet should block oxygen ignition.");
            Assert.That(flammable.OnFire, Is.False);
        });
    }
}
