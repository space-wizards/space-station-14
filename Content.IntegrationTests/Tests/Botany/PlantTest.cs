#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.IntegrationTests.Tests.Toolshed;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests.Tests.Botany;

[TestFixture]
public sealed class PlantTest : ToolshedTest
{
    private const string BotanistGloves = "ClothingHandsGlovesLeather";
    private const string Hatchet = "HydroponicsToolHatchet";
    private const string Human = "MobHuman";

    [Test]
    public async Task AllSeedsCanBePlanted()
    {
        await CreateBotanyTestMap();

        await Server.WaitAssertion(() =>
        {
            foreach (var seed in GetSeedPrototypes())
            {
                var plant = InvokeCommand<EntityUid>($"var $coordinates plant:spawn {seed.ID}");

                Assert.That(plant.IsValid(), Is.True, $"Seed {seed.ID} did not produce a valid plant entity.");
                Assert.That(Server.EntMan.HasComponent<PlantComponent>(plant), Is.True,
                    $"Seed {seed.ID} did not produce an entity with {nameof(PlantComponent)}.");
                Assert.That(Server.EntMan.HasComponent<PlantHolderComponent>(plant), Is.True,
                    $"Seed {seed.ID} did not produce an entity with {nameof(PlantHolderComponent)}.");
            }
        });
    }

    [Test]
    public async Task AllSeedsCanReachHarvestWithoutDying()
    {
        await CreateBotanyTestMap();

        await Server.WaitAssertion(() =>
        {
            var plantHolder = Server.System<PlantHolderSystem>();

            foreach (var seed in GetSeedPrototypes())
            {
                var plant = InvokeCommand<EntityUid>($"var $coordinates plant:spawn {seed.ID}");
                WriteVar("plant", plant);
                var agedPlant = InvokeCommand<EntityUid>("var $plant plant:ageuntilready");

                Assert.That(agedPlant, Is.EqualTo(plant),
                    $"Plant grown from {seed.ID} was not returned by plant:ageuntilready.");
                Assert.That(Server.EntMan.TryGetComponent(plant, out PlantHolderComponent? holder), Is.True,
                    $"Plant grown from {seed.ID} lost its {nameof(PlantHolderComponent)} while aging.");
                Assert.That(plantHolder.IsDead((plant, holder)), Is.False,
                    $"Plant grown from {seed.ID} died before it could be harvested.");
                Assert.That(holder!.ReadyForHarvest, Is.True,
                    $"Plant grown from {seed.ID} is not ready for harvest after plant:ageuntilready.");
            }
        });
    }

    [Test]
    public async Task AllPlantsCanBeHarvestedWithBotanistEquipment()
    {
        var map = await CreateBotanyTestMap();

        await Server.WaitAssertion(() =>
        {
            var entityManager = Server.EntMan;
            var hands = Server.System<SharedHandsSystem>();
            var interaction = Server.System<SharedInteractionSystem>();
            var inventory = Server.System<InventorySystem>();

            var botanist = entityManager.SpawnEntity(Human, map.GridCoords);
            var gloves = entityManager.SpawnEntity(BotanistGloves, map.GridCoords);
            var hatchet = entityManager.SpawnEntity(Hatchet, map.GridCoords);

            Assert.That(inventory.TryEquip(botanist, gloves, "gloves"), Is.True,
                "Failed to equip the botanist's gloves.");
            Assert.That(hands.TryPickupAnyHand(botanist, hatchet), Is.True,
                "Failed to put the hatchet in the botanist's hand.");

            foreach (var seed in GetSeedPrototypes())
            {
                var plant = InvokeCommand<EntityUid>($"var $coordinates plant:spawn {seed.ID}");
                WriteVar("plant", plant);
                InvokeCommand<EntityUid>("var $plant plant:ageuntilready");

                if (entityManager.HasComponent<PlantTraitLigneousComponent>(plant))
                {
                    interaction.InteractUsing(
                        botanist,
                        hatchet,
                        plant,
                        entityManager.GetComponent<TransformComponent>(plant).Coordinates,
                        checkCanInteract: false,
                        checkCanUse: false);
                }
                else
                {
                    interaction.InteractHand(botanist, plant);
                }

                if (entityManager.Deleted(plant))
                    continue;

                Assert.That(entityManager.TryGetComponent(plant, out PlantHolderComponent? holder), Is.True,
                    $"Plant grown from {seed.ID} lost its {nameof(PlantHolderComponent)} when harvested.");
                Assert.That(holder!.ReadyForHarvest, Is.False,
                    $"Plant grown from {seed.ID} was not harvested.");
            }
        });
    }

    [Test]
    public async Task AllLigneousPlantsRequireHatchetToHarvest()
    {
        var map = await CreateBotanyTestMap();

        await Server.WaitAssertion(() =>
        {
            var entityManager = Server.EntMan;
            var interaction = Server.System<SharedInteractionSystem>();
            var botanist = entityManager.SpawnEntity(Human, map.GridCoords);

            foreach (var seed in GetSeedPrototypes())
            {
                var plant = InvokeCommand<EntityUid>($"var $coordinates plant:spawn {seed.ID}");
                WriteVar("plant", plant);

                if (!entityManager.HasComponent<PlantTraitLigneousComponent>(plant))
                {
                    var mutatedPlant = InvokeCommand<EntityUid>(
                        "var $plant plantmutation:add RandomPlantMutations \"Lignification\"");
                    Assert.That(mutatedPlant, Is.EqualTo(plant),
                        $"Plant grown from {seed.ID} could not be made ligneous.");
                }

                Assert.That(entityManager.HasComponent<PlantTraitLigneousComponent>(plant), Is.True,
                    $"Plant grown from {seed.ID} does not have {nameof(PlantTraitLigneousComponent)}.");

                InvokeCommand<EntityUid>("var $plant plant:ageuntilready");
                interaction.InteractHand(botanist, plant);

                Assert.That(entityManager.Deleted(plant), Is.False,
                    $"Ligneous plant grown from {seed.ID} was harvested without a hatchet.");
                Assert.That(entityManager.TryGetComponent(plant, out PlantHolderComponent? holder), Is.True,
                    $"Ligneous plant grown from {seed.ID} lost its {nameof(PlantHolderComponent)}.");
                Assert.That(holder!.ReadyForHarvest, Is.True,
                    $"Ligneous plant grown from {seed.ID} was harvested without a hatchet.");
            }
        });
    }

    private async Task<TestMapData> CreateBotanyTestMap()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            var atmosphere = Server.System<AtmosphereSystem>();
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int)Gas.Oxygen] = 21.824779f;
            moles[(int)Gas.Nitrogen] = 82.10312f;
            atmosphere.SetMapAtmosphere(map.MapUid, false, new GasMixture(moles, Atmospherics.T20C));
            WriteVar("coordinates", map.GridCoords);
        });

        return map;
    }

    private List<EntityPrototype> GetSeedPrototypes()
    {
        var componentFactory = Server.ResolveDependency<IComponentFactory>();

        return Server.ProtoMan.EnumeratePrototypes<EntityPrototype>()
            .Where(prototype => !prototype.Abstract)
            .Where(prototype => !Pair.IsTestPrototype(prototype))
            .Where(prototype => prototype.HasComp<SeedComponent>(componentFactory))
            .OrderBy(prototype => prototype.ID)
            .ToList();
    }

}
