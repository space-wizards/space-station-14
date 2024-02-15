using Content.Server.Botany.Components;
using Content.Server.PowerCell;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.PlantAnalyzer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Threading;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private Dictionary<EntityUid, CancellationTokenWrapper> _cancellationTokenSources = new Dictionary<EntityUid, CancellationTokenWrapper>();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerSetMode>(OnModeSelected);
    }

    private void OnAfterInteract(Entity<PlantAnalyzerComponent> plantAnalyzer, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<SeedComponent>(args.Target) && !HasComp<PlantHolderComponent>(args.Target) || !_cell.HasActivatableCharge(plantAnalyzer, user: args.User))
            return;

        _audio.PlayPvs(plantAnalyzer.Comp.ScanningBeginSound, plantAnalyzer);

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationTokenWrapper = new CancellationTokenWrapper(cancellationTokenSource);

        _cancellationTokenSources[plantAnalyzer] = cancellationTokenWrapper;

        if (plantAnalyzer.Comp.AdvancedScan)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, plantAnalyzer.Comp.AdvScanDelay,
                new PlantAnalyzerDoAfterEvent { CancellationTokenWrapper = cancellationTokenWrapper }, plantAnalyzer, target: args.Target, used: plantAnalyzer)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            });
        }
        else
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, plantAnalyzer.Comp.ScanDelay,
                new PlantAnalyzerDoAfterEvent { CancellationTokenWrapper = cancellationTokenWrapper }, plantAnalyzer, target: args.Target, used: plantAnalyzer)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }
    }

    private void OnDoAfter(Entity<PlantAnalyzerComponent> component, ref PlantAnalyzerDoAfterEvent args)
    {
        if (component.Comp.AdvancedScan) // Double charge use for advanced scan.
        {
            if (!_cell.TryUseActivatableCharge(component, user: args.User))
                return;
        }
        if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(component.Owner, user: args.User))
            return;

        _audio.PlayPvs(component.Comp.ScanningEndSound, args.Args.User);

        OpenUserInterface(args.User, component);
        UpdateScannedUser(component, args.Args.User, args.Args.Target.Value, component);

        args.Handled = true;
        _cancellationTokenSources.Remove(component);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.TryGetUi(analyzer, PlantAnalyzerUiKey.Key, out var ui))
            return;

        _uiSystem.OpenUi(ui, actor.PlayerSession);
    }

    public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid target, Entity<PlantAnalyzerComponent> component)
    {
        if (!_uiSystem.TryGetUi(uid, PlantAnalyzerUiKey.Key, out var ui))
            return;

        TryComp<PlantHolderComponent>(target, out var plantcomp);
        TryComp<SeedComponent>(target, out var seedcomponent);

        var state = default(PlantAnalyzerScannedSeedPlantInformation);
        var seedData = default(SeedData); // Data of a unique Seed.
        var seedProtoId = default(SeedPrototype); // Data of base seed prototype.

        if (seedcomponent != null)
        {
            if (seedcomponent.Seed != null) // If unique seed.
            {
                seedData = seedcomponent.Seed;
                state = ObtainingGeneDataSeed(seedData, target, false, component.Comp.AdvancedScan);
            }
            else if (seedcomponent.SeedId != null && _prototypeManager.TryIndex(seedcomponent.SeedId, out SeedPrototype? protoSeed)) // Get the seed protoype.
            {
                seedProtoId = protoSeed;
                state = ObtainingGeneDataSeedProt(protoSeed, target, component.Comp.AdvancedScan);
            }
        }
        else if (plantcomp != null) // If we poke the plantholder, it checks the plantholder seed.
        {
            seedData = plantcomp.Seed;
            if (seedData != null)
            {
                state = ObtainingGeneDataSeed(seedData, target, true, component.Comp.AdvancedScan);  // SeedData is a unique seed in a tray.
            }
        }

        if (state == null)
            return;

        _uiSystem.SendUiMessage(ui, state);
    }

    /// <summary>
    ///     Analysis of seed from prototype.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeedProt(SeedPrototype comp, EntityUid target, Boolean scanMode)
    {
        var seedName = "\n  " + Loc.GetString("plant-analyzer-window-label-trademark") +
            "\n    " + Loc.GetString(comp.DisplayName);

        var seedChem = "\n   ";
        var plantHarvestType = "";
        var exudeGases = "";

        if (comp.HarvestRepeat == HarvestType.Repeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-repeat");
        if (comp.HarvestRepeat == HarvestType.NoRepeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-ephemeral");
        if (comp.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = Loc.GetString("plant-analyzer-harvest-autoharvest");

        seedChem += String.Join("\n   ", comp.Chemicals.Select(item => item.Key.ToString()));

        exudeGases = comp.ExudeGasses.Count > 0
            ? string.Join("\n   ", comp.ExudeGasses.Keys)
            : Loc.GetString("plant-analyzer-plant-gasses-no");

        List<string> tolerancesList = CheckAllTolerances(comp);
        List<string> generalTraitsList = CheckGeneralTraits(comp);
        List<string> mutationsList = CheckAllMutations(comp);

        return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), seedName,
            seedChem, plantHarvestType, exudeGases, mutationsList, false, tolerancesList, generalTraitsList, scanMode);
    }

    /// <summary>
    ///     Analysis of unique seed.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(SeedData comp, EntityUid target, bool trayChecker, Boolean scanMode)
    {
        var seedName = Loc.GetString(comp.DisplayName);

        var seedChem = "\n   ";
        var plantHarvestType = "";
        var exudeGases = "";

        if (comp.HarvestRepeat == HarvestType.Repeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-repeat");
        if (comp.HarvestRepeat == HarvestType.NoRepeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-ephemeral");
        if (comp.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = Loc.GetString("plant-analyzer-harvest-autoharvest");

        seedChem += string.Join("\n   ", comp.Chemicals.Select(item => item.Key.ToString()));

        exudeGases = comp.ExudeGasses.Count > 0
            ? string.Join("\n   ", comp.ExudeGasses.Keys)
            : Loc.GetString("plant-analyzer-plant-gasses-no");

        List<string> tolerancesList = new List<string>();
        List<string> mutationsList = new List<string>();

        if (scanMode)
        {
            tolerancesList = CheckAllTolerances(comp);
            mutationsList = CheckAllMutations(comp);
        }

        List<string> generalTraitsList = CheckGeneralTraits(comp);

        return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), seedName,
            seedChem, plantHarvestType, exudeGases, mutationsList, trayChecker, tolerancesList, generalTraitsList, scanMode);
    }

    /// <summary>
    ///     Returns information about the seed.
    /// </summary>
    public List<string> CheckGeneralTraits(SeedData plant)
    {
        var name = Loc.GetString(plant.DisplayName);
        float endurance = plant.Endurance;
        float yield = plant.Yield;
        float lifespan = plant.Lifespan;
        float maturation = plant.Maturation;
        float growthStages = plant.GrowthStages;
        float potency = plant.Potency;
        return new List<string> { name, endurance.ToString(), yield.ToString(), lifespan.ToString(), maturation.ToString(), growthStages.ToString(), potency.ToString() };
    }

    public List<string> CheckAllMutations(SeedData plant)
    {
        string plantMutations = "";
        string plantSpeciation = "";
        plantSpeciation += String.Join(", \n", plant.MutationPrototypes.Select(item => item.ToString()));

        if (plant.TurnIntoKudzu) plantMutations += "\n   " + $"{Loc.GetString("plant-analyzer-mutation-turnintokudzu")}";
        if (plant.Seedless) plantMutations += "\n   " + $"{Loc.GetString("plant-analyzer-mutation-seedless")}";
        if (plant.Slip) plantMutations += "\n   " + $"{Loc.GetString("plant-analyzer-mutation-slip")}";
        if (plant.Sentient) plantMutations += "\n   " + $"{Loc.GetString("plant-analyzer-mutation-sentient")}";
        if (plant.Ligneous) plantMutations += "\n   " + $"{Loc.GetString("plant-analyzer-mutation-ligneous")}";
        if (plant.Bioluminescent) plantMutations += "\n   " + $"{Loc.GetString("plant-analyzer-mutation-bioluminescent")}";
        if (plant.CanScream) plantMutations += "\n   " + $"{Loc.GetString("plant-analyzer-mutation-canscream")}";
        return new List<string> { plantMutations, plantSpeciation };
    }

    public List<string> CheckAllTolerances(SeedData plant)
    {
        var nutrientConsumption = plant.NutrientConsumption;
        var waterConsumption = plant.NutrientConsumption;
        var idealHeat = plant.IdealHeat;
        var heatTolerance = plant.HeatTolerance;
        var idealLight = plant.IdealLight;
        var lightTolerance = plant.LightTolerance;
        var toxinsTolerance = plant.ToxinsTolerance;
        var lowPresssureTolerance = plant.LowPressureTolerance;
        var highPressureTolerance = plant.HighPressureTolerance;
        var pestTolerance = plant.PestTolerance;
        var weedTolerance = plant.WeedTolerance;
        return new List<string> { nutrientConsumption.ToString(), waterConsumption.ToString(), idealHeat.ToString(), heatTolerance.ToString(), idealLight.ToString(),
            lightTolerance.ToString(), toxinsTolerance.ToString(),lowPresssureTolerance.ToString(),highPressureTolerance.ToString(),pestTolerance.ToString(), weedTolerance.ToString() };
    }

    private void OnModeSelected(Entity<PlantAnalyzerComponent> component, ref PlantAnalyzerSetMode args)
    {
        SetMode(component, args.AdvancedScan);
    }

    public void SetMode(Entity<PlantAnalyzerComponent> component, bool isAdvMode)
    {
        // Prevents switching to advanced mode if doAfter is already running but not vice versa.
        if (_cancellationTokenSources.ContainsKey(component) && isAdvMode)
            return;

        component.Comp.AdvancedScan = isAdvMode;
    }
}
