using System.Diagnostics.CodeAnalysis;
using Content.Server.AbstractAnalyzer;
using Content.Server.Botany.Components;
using Content.Shared.Botany.PlantAnalyzer;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : AbstractAnalyzerSystem<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <inheritdoc/>
    public override void UpdateScannedUser(EntityUid analyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(analyzer, PlantAnalyzerUiKey.Key))
            return;

        if (!ValidScanTarget(target))
            return;

        PlantAnalyzerPlantData? plantData = null;
        PlantAnalyzerTrayData? trayData = null;
        PlantAnalyzerTolerancesData? tolerancesData = null;
        PlantAnalyzerProduceData? produceData = null;
        if (_entityManager.TryGetComponent<PlantHolderComponent>(target, out var plantHolder))
        {
            if (plantHolder.Seed is not null)
            {
                plantData = new PlantAnalyzerPlantData(
                    seedDisplayName: plantHolder.Seed.DisplayName,
                    health: plantHolder.Health,
                    endurance: plantHolder.Seed.Endurance,
                    age: plantHolder.Age,
                    lifespan: plantHolder.Seed.Lifespan,
                    dead: plantHolder.Dead,
                    viable: plantHolder.Seed.Viable,
                    mutating: plantHolder.MutationLevel > 0f
                );
                tolerancesData = new PlantAnalyzerTolerancesData(
                    waterConsumption: plantHolder.Seed.WaterConsumption,
                    nutrientConsumption: plantHolder.Seed.NutrientConsumption,
                    toxinsTolerance: plantHolder.Seed.ToxinsTolerance,
                    pestTolerance: plantHolder.Seed.PestTolerance,
                    weedTolerance: plantHolder.Seed.WeedTolerance,
                    lowPressureTolerance: plantHolder.Seed.LowPressureTolerance,
                    highPressureTolerance: plantHolder.Seed.HighPressureTolerance,
                    idealHeat: plantHolder.Seed.IdealHeat,
                    heatTolerance: plantHolder.Seed.HeatTolerance,
                    idealLight: plantHolder.Seed.IdealLight,
                    lightTolerance: plantHolder.Seed.LightTolerance,
                    consumeGasses: [.. plantHolder.Seed.ConsumeGasses.Keys]
                );
                produceData = new PlantAnalyzerProduceData(
                    yield: BotanySystem.CalculateTotalYield(plantHolder.Seed.Yield, plantHolder.YieldMod),
                    potency: plantHolder.Seed.Potency,
                    chemicals: [.. plantHolder.Seed.Chemicals.Keys],
                    produce: plantHolder.Seed.ProductPrototypes,
                    exudeGasses: [.. plantHolder.Seed.ExudeGasses.Keys]
                );
            }
            trayData = new PlantAnalyzerTrayData(
                waterLevel: plantHolder.WaterLevel,
                nutritionLevel: plantHolder.NutritionLevel,
                toxins: plantHolder.Toxins,
                pestLevel: plantHolder.PestLevel,
                weedLevel: plantHolder.WeedLevel
            );
        }

        _uiSystem.ServerSendUiMessage(analyzer, PlantAnalyzerUiKey.Key, new PlantAnalyzerScannedUserMessage(
            GetNetEntity(target),
            scanMode,
            plantData,
            trayData,
            tolerancesData,
            produceData
        ));
    }

    /// <inheritdoc/>
    protected override Enum GetUiKey()
    {
        return PlantAnalyzerUiKey.Key;
    }

    /// <inheritdoc/>
    protected override bool ScanTargetPopupMessage(Entity<PlantAnalyzerComponent> uid, AfterInteractEvent args, [NotNullWhen(true)] out string? message)
    {
        message = null;
        return false;
    }

    /// <inheritdoc/>
    protected override bool ValidScanTarget(EntityUid? target)
    {
        return HasComp<PlantHolderComponent>(target);
    }
}
