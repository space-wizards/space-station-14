using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Server.AbstractAnalyzer;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Botany.PlantAnalyzer;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : AbstractAnalyzerSystem<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerPrintMessage>(OnPrint);
    }

    /// <inheritdoc/>
    public override void UpdateScannedUser(EntityUid analyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(analyzer, PlantAnalyzerUiKey.Key))
            return;

        if (!ValidScanTarget(target))
            return;

        if (!_entityManager.TryGetComponent<PlantAnalyzerComponent>(analyzer, out var analyzerComponent))
            return;

        _uiSystem.ServerSendUiMessage(analyzer, PlantAnalyzerUiKey.Key, GatherData(analyzerComponent, scanMode, target: target));
    }

    private PlantAnalyzerScannedUserMessage GatherData(PlantAnalyzerComponent analyzer, bool? scanMode = null, EntityUid? target = null)
    {
        target ??= analyzer.ScannedEntity;
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
                    exudeGasses: [.. plantHolder.Seed.ExudeGasses.Keys],
                    seedless: plantHolder.Seed.Seedless
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

        return new PlantAnalyzerScannedUserMessage(
            GetNetEntity(target),
            scanMode,
            plantData,
            trayData,
            tolerancesData,
            produceData,
            analyzer.PrintReadyAt
        );
    }

    private void OnPrint(EntityUid uid, PlantAnalyzerComponent component, PlantAnalyzerPrintMessage args)
    {
        var user = args.Actor;

        if (_gameTiming.CurTime < component.PrintReadyAt)
        {
            // This shouldn't occur due to the UI guarding against it, but
            // if it does, tell the user why nothing happened.
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), uid, user);
            return;
        }

        // Spawn a piece of paper.
        var printed = EntityManager.SpawnEntity(component.MachineOutput, Transform(uid).Coordinates);
        _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            Log.Error("Printed paper did not have PaperComponent.");
            return;
        }

        var data = GatherData(component);
        var missingDataFtl = "plant-analyzer-printout-missing";
        var missingData = Loc.GetString(missingDataFtl);

        (string, string)[] parameters = [
            ("seedName", Loc.GetString(data.PlantData?.SeedDisplayName ?? missingDataFtl)),
            ("produce", data.ProduceData is not null ? PlantAnalyzerLocalizationHelper.ProduceToLocalizedStrings(data.ProduceData.Produce, _prototypeManager).Plural : missingData),
            ("water", data.TolerancesData?.WaterConsumption.ToString("0.00") ?? missingData),
            ("nutrients", data.TolerancesData?.NutrientConsumption.ToString("0.00") ?? missingData),
            ("toxins", data.TolerancesData?.ToxinsTolerance.ToString("0.00") ?? missingData),
            ("pests", data.TolerancesData?.PestTolerance.ToString("0.00") ?? missingData),
            ("weeds", data.TolerancesData?.WeedTolerance.ToString("0.00") ?? missingData),
            ("gasesIn", data.TolerancesData is not null ? PlantAnalyzerLocalizationHelper.GasesToLocalizedStrings(data.TolerancesData.ConsumeGasses, _prototypeManager) : missingData),
            ("kpa", data.TolerancesData?.IdealPressure.ToString("0.00") ?? missingData),
            ("kpaTolerance", data.TolerancesData?.PressureTolerance.ToString("0.00") ?? missingData),
            ("temp", data.TolerancesData?.IdealHeat.ToString("0.00") ?? missingData),
            ("tempTolerance", data.TolerancesData?.HeatTolerance.ToString("0.00") ?? missingData),
            ("lightLevel", data.TolerancesData?.IdealLight.ToString("0.00") ?? missingData),
            ("lightTolerance", data.TolerancesData?.LightTolerance.ToString("0.00") ?? missingData),
            ("n", data.ProduceData?.Yield.ToString("0") ?? missingData),
            ("potency", Loc.GetString(data.ProduceData?.Potency ?? missingDataFtl)),
            ("chemicals", data.ProduceData is not null ? PlantAnalyzerLocalizationHelper.ChemicalsToLocalizedStrings(data.ProduceData.Chemicals, _prototypeManager) : missingData),
            ("gasesOut", data.ProduceData is not null ? PlantAnalyzerLocalizationHelper.GasesToLocalizedStrings(data.ProduceData.ExudeGasses, _prototypeManager) : missingData),
            ("endurance", data.PlantData?.Endurance.ToString("0.00") ?? missingData),
            ("lifespan", data.PlantData?.Lifespan.ToString("0.00") ?? missingData),
            ("seedless", data.ProduceData is not null ? (data.ProduceData.Seedless ? "yes" : "no") : "other"),
            ("indent", "    ")
        ];
        var text = new StringBuilder();
        for (var i = 0; i < 21; i++)
            text.AppendLine(Loc.GetString($"plant-analyzer-printout-l{i}", [.. parameters]));

        _paperSystem.SetContent((printed, paperComp), text.ToString().TrimEnd('\r', '\n'));
        _audioSystem.PlayPvs(component.SoundPrint, uid,
            AudioParams.Default
            .WithVariation(0.25f)
            .WithVolume(3f)
            .WithRolloffFactor(2.8f)
            .WithMaxDistance(4.5f));

        component.PrintReadyAt = _gameTiming.CurTime + component.PrintCooldown;
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
