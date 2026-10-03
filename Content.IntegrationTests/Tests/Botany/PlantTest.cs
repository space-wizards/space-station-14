#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Botany;

[TestFixture]
public sealed class PlantTest : GameTest
{
    private const string BotanistGloves = "ClothingHandsGlovesLeather";
    private const string Hatchet = "HydroponicsToolHatchet";
    private const string Human = "MobHuman";

    public override PoolSettings PoolSettings => new() { Connected = false };

    public override async Task DoSetup()
    {
        await base.DoSetup();
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            var atmosphere = Server.System<AtmosphereSystem>();
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int)Gas.Oxygen] = 21.824779f;
            moles[(int)Gas.Nitrogen] = 82.10312f;
            atmosphere.SetMapAtmosphere(map.MapUid, false, new GasMixture(moles, Atmospherics.T20C));
        });
    }

    [Test, RunOnSide(Side.Server)]
    public void AllPlantsCanBeHarvestedWithBotanistEquipment()
    {
        var map = TestMap!;

        var entityManager = Server.EntMan;
        var hands = Server.System<SharedHandsSystem>();
        var interaction = Server.System<SharedInteractionSystem>();
        var inventory = Server.System<InventorySystem>();

        var botanist = entityManager.SpawnEntity(Human, map.GridCoords);
        var gloves = entityManager.SpawnEntity(BotanistGloves, map.GridCoords);
        var hatchet = entityManager.SpawnEntity(Hatchet, map.GridCoords);

        inventory.TryEquip(botanist, gloves, "gloves");
        hands.TryPickupAnyHand(botanist, hatchet);

        foreach (var seed in GetSeedPrototypes())
        {
            var plant = SpawnHarvestablePlant(seed);

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
    }

    [Test, RunOnSide(Side.Server)]
    public void AllLigneousPlantsRequireHatchetToHarvest()
    {
        var map = TestMap!;

        var prototypeManager = Server.ProtoMan;
        var entityManager = Server.EntMan;
        var componentFactory = Server.ResolveDependency<IComponentFactory>();
        var interaction = Server.System<SharedInteractionSystem>();
        var botanist = entityManager.SpawnEntity(Human, map.GridCoords);
        var seeds = GetSeedPrototypes()
                        .Where(seed =>
                        {
                            if (!seed.TryComp<SeedComponent>(out var seedComponent, componentFactory)) return false;
                            return prototypeManager.Index(seedComponent.PlantProtoId).HasComp<PlantTraitLigneousComponent>(componentFactory);
                        });
        foreach (var seed in seeds)
        {
            var plant = SpawnHarvestablePlant(seed);
            interaction.InteractHand(botanist, plant);

            Assert.That(entityManager.Deleted(plant), Is.False,
                $"Ligneous plant grown from {seed.ID} was harvested without a hatchet.");
            Assert.That(entityManager.TryGetComponent(plant, out PlantHolderComponent? holder), Is.True,
                $"Ligneous plant grown from {seed.ID} lost its {nameof(PlantHolderComponent)}.");
            Assert.That(holder!.ReadyForHarvest, Is.True,
                $"Ligneous plant grown from {seed.ID} was harvested without a hatchet.");
        }
    }

    private EntityUid SpawnHarvestablePlant(EntityPrototype seed)
    {
        var entityManager = Server.EntMan;
        var componentFactory = Server.ResolveDependency<IComponentFactory>();
        Assert.That(seed.TryComp<SeedComponent>(out var seedComponent, componentFactory), Is.True);

        var tray = entityManager.SpawnEntity("hydroponicsTray", TestMap!.GridCoords);
        var plant = entityManager.SpawnEntity(seedComponent!.PlantProtoId, TestMap.GridCoords);
        Server.System<BotanySystem>().ApplyPlantSnapshotData(seedComponent.PlantData, plant);
        Server.System<PlantTraySystem>().PlantingPlantInTray(tray, plant);

        var plantComponent = entityManager.GetComponent<PlantComponent>(plant);
        var holder = entityManager.GetComponent<PlantHolderComponent>(plant);
        var readyAge = (int)MathF.Ceiling(plantComponent.Maturation) + (int)MathF.Floor(plantComponent.Production) + 1;
        Server.System<PlantHolderSystem>().AdjustsAge((plant, holder), readyAge - holder.Age);
        Server.System<PlantSystem>().ForceUpdate((plant, plantComponent));
        Assert.That(holder.ReadyForHarvest, Is.True, $"Plant grown from {seed.ID} was not ready for harvest.");

        return plant;
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
