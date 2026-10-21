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
    private readonly EntProtoId _botanistGloves = "ClothingHandsGlovesLeather";
    private readonly EntProtoId _hatchet = "HydroponicsToolHatchet";
    private readonly EntProtoId _human = "MobHuman";

    [SidedDependency(Side.Server)]
    private readonly SharedHandsSystem _sHands = null!;

    [SidedDependency(Side.Server)]
    private readonly SharedInteractionSystem _sInteraction = null!;

    [SidedDependency(Side.Server)]
    private readonly InventorySystem _sInventory = null!;

    [SidedDependency(Side.Server)]
    private readonly PlantHolderSystem _sPlantHolder = null!;

    [SidedDependency(Side.Server)]
    private readonly PlantSystem _sPlant = null!;


    public override PoolSettings PoolSettings => new() { Connected = false };

    public override async Task DoSetup()
    {
        await base.DoSetup();
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            var atmosphere = Server.System<AtmosphereSystem>();
            var mixture = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
            mixture.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
            mixture.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
            atmosphere.SetMapAtmosphere(map.MapUid, false, mixture);
        });
    }

    [Test, RunOnSide(Side.Server)]
    public void AllPlantsCanBeHarvestedWithBotanistEquipment()
    {
        Assume.That(TestMap != null);
        var map = TestMap;

        var entityManager = Server.EntMan;

        var botanist = SSpawnAtPosition(_human, map.GridCoords);
        var gloves = SSpawnAtPosition(_botanistGloves, map.GridCoords);
        var hatchet = SSpawnAtPosition(_hatchet, map.GridCoords);

        _sInventory.TryEquip(botanist, gloves, "gloves");
        _sHands.TryPickupAnyHand(botanist, hatchet);

        Assert.Multiple(() =>
        {
            foreach (var seed in GetSeedPrototypes())
            {
                var plant = SpawnHarvestablePlant(seed);

                if (entityManager.HasComponent<PlantTraitLigneousComponent>(plant))
                {
                    _sInteraction.InteractUsing(
                        botanist,
                        hatchet,
                        plant,
                        entityManager.GetComponent<TransformComponent>(plant).Coordinates,
                        checkCanInteract: false,
                        checkCanUse: false);
                }
                else
                {
                    _sInteraction.InteractHand(botanist, plant);
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

    [Test, RunOnSide(Side.Server)]
    public void AllLigneousPlantsRequireHatchetToHarvest()
    {
        var map = TestMap!;

        var prototypeManager = SProtoMan;
        var entityManager = SEntMan;
        var componentFactory = Server.ResolveDependency<IComponentFactory>();
        var botanist = SSpawnAtPosition(_human, map.GridCoords);

        var gloves = SSpawnAtPosition(_botanistGloves, map.GridCoords);
        _sInventory.TryEquip(botanist, gloves, "gloves");

        var seeds = GetSeedPrototypes()
                        .Where(seed =>
                        {
                            if (!seed.TryComp<SeedComponent>(out var seedComponent, componentFactory))
                                return false;
                            return prototypeManager.Index(seedComponent.PlantProtoId).HasComp<PlantTraitLigneousComponent>(componentFactory);
                        });
        Assert.Multiple(() =>
        {
            foreach (var seed in seeds)
            {
                var plant = SpawnHarvestablePlant(seed);
                _sInteraction.InteractHand(botanist, plant);

                Assert.That(entityManager.Deleted(plant), Is.False,
                    $"Ligneous plant grown from {seed.ID} was harvested without a hatchet.");
                Assert.That(entityManager.TryGetComponent(plant, out PlantHolderComponent? holder), Is.True,
                    $"Ligneous plant grown from {seed.ID} lost its {nameof(PlantHolderComponent)}.");
                if (holder == null)
                    continue; // Already failed on the assert, but since multiple we want to see the other failures in the loop
                Assert.That(holder!.ReadyForHarvest, Is.True,
                    $"Ligneous plant grown from {seed.ID} was harvested without a hatchet.");
            }
        });
    }

    private EntityUid SpawnHarvestablePlant(EntityPrototype seed)
    {
        var entityManager = Server.EntMan;
        var componentFactory = Server.ResolveDependency<IComponentFactory>();
        Assert.That(seed.TryComp<SeedComponent>(out var seedComponent, componentFactory), Is.True);

        var tray = SSpawnAtPosition("HydroponicsTray", TestMap!.GridCoords);
        var plant = SSpawnAtPosition(seedComponent!.PlantProtoId, TestMap.GridCoords);
        Server.System<BotanySystem>().ApplyPlantSnapshotData(seedComponent.PlantData, plant);
        Server.System<PlantTraySystem>().PlantingPlantInTray(tray, plant);

        var plantComponent = entityManager.GetComponent<PlantComponent>(plant);
        var holder = entityManager.GetComponent<PlantHolderComponent>(plant);
        var readyAge = (int)MathF.Ceiling(plantComponent.Maturation) + (int)MathF.Floor(plantComponent.Production) + 1;
        _sPlantHolder.AdjustsAge((plant, holder), readyAge - holder.Age);
        _sPlant.ForceUpdate((plant, plantComponent));
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
