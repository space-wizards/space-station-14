using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.PowerCell;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.PlantAnalyzer;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private PlantHolderComponent _plantHolder = default!;
    private Boolean _scanMode;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerSetMode>(OnModeSelected);
    }

    private void OnAfterInteract(EntityUid uid, PlantAnalyzerComponent plantAnalyzer, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<SeedComponent>(args.Target) && !HasComp<PlantHolderComponent>(args.Target) || !_cell.HasActivatableCharge(uid, user: args.User))
            return;
        _audio.PlayPvs(plantAnalyzer.ScanningBeginSound, uid);

        _scanMode = plantAnalyzer.AdvancedScan;
        if (plantAnalyzer.AdvancedScan)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, plantAnalyzer.AdvScanDelay, new PlantAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }
        else
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, plantAnalyzer.ScanDelay, new PlantAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }

    }

    private void OnDoAfter(EntityUid uid, PlantAnalyzerComponent component, DoAfterEvent args)
    {
        if (component.AdvancedScan) //double charge use for advanced scan to 80 usage which is of ~22% small cell
        {
            if (!_cell.TryUseActivatableCharge(uid, user: args.User))
                return;
        }
        if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(uid, user: args.User))
            return;

        _audio.PlayPvs(component.ScanningEndSound, args.Args.User);

        UpdateScannedUser(uid, args.Args.User, args.Args.Target.Value, component);
        args.Handled = true;
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.TryGetUi(analyzer, PlantAnalyzerUiKey.Key, out var ui))
            return;

        _uiSystem.OpenUi(ui, actor.PlayerSession);
    }

    public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid target, PlantAnalyzerComponent? plantAnalyzer)
    {
        if (!Resolve(uid, ref plantAnalyzer))
            return;
        if (target == null || !_uiSystem.TryGetUi(uid, PlantAnalyzerUiKey.Key, out var ui))
            return;

        TryComp<PlantHolderComponent>(target, out var plantcomp);
        TryComp<SeedComponent>(target, out var seedcomponent);

        var state = default(PlantAnalyzerScannedSeedPlantInformation);
        var seedData = default(SeedData);   // data of a unique Seed
        var seedProtoId = default(SeedPrototype);   // data of base seed prototype

        if (seedcomponent != null)
        {
            if (seedcomponent.Seed != null) //if unqiue seed
            {
                seedData = seedcomponent.Seed;
                state = ObtainingGeneDataSeed(seedData, target, false);
            }
            else if (seedcomponent.SeedId != null && _prototypeManager.TryIndex(seedcomponent.SeedId, out SeedPrototype? protoSeed)) // getting the protoype seed
            {
                seedProtoId = protoSeed;
                state = ObtainingGeneDataSeedProt(protoSeed, target);
            }
        }
        else if (plantcomp != null)    //where check if we poke the plantholder, it checks the plantholder seed
        {
            _plantHolder = plantcomp;
            seedData = plantcomp.Seed;
            if (seedData != null)
            {
                state = ObtainingGeneDataSeed(seedData, target, true); //seedData is a unique seed in a tray
            }
        }

        if (state == null)
            return;
        OpenUserInterface(user, uid);
        _uiSystem.SendUiMessage(ui, state);
    }

    /// <summary>
    ///     Analysis of seed from prototype. Shows all information since they are the "known stock" of NT.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeedProt(SeedPrototype comp, EntityUid target)
    {
        var seedName = "\r\n  NanoTrasen owned prototype: \r\n  " + Loc.GetString(comp.DisplayName) + " (tm)";
        //var seedName = comp.Name;

        var seedChem = "\r\n   ";
        var plantHarvestType = "";
        var exudeGases = "";
        var potency = comp.Potency;
        var yield = comp.Yield;

        var plantSpeciation = "";
        var plantMutations = "";
        var tolerances = "";
        var generalTraits = "";

        if (comp.HarvestRepeat == HarvestType.Repeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-repeat");
        if (comp.HarvestRepeat == HarvestType.NoRepeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-ephemeral");
        if (comp.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = Loc.GetString("plant-analyzer-harvest-autoharvest");

        seedChem += String.Join("\r\n   ", comp.Chemicals.Select(item => item.Key.ToString()));

        if (comp.ExudeGasses.Count > 0)
        {
            foreach (var (gas, amount) in comp.ExudeGasses)
            {
                exudeGases += "\r\n   " + gas;
            };
        }
        else
        {
            exudeGases = Loc.GetString("plant-analyzer-plant-gasses-No");
        }

        plantSpeciation += String.Join(", \r\n", comp.MutationPrototypes.Select(item => item.ToString()));
        plantMutations = CheckAllMutations(comp, plantMutations);
        tolerances = CheckAllTolerances(comp, tolerances);

        generalTraits = CheckAllGeneralTraits(comp, generalTraits);

        return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), seedName,
            seedChem, plantHarvestType, exudeGases, potency.ToString(), yield.ToString(), plantMutations, false, plantSpeciation, tolerances.ToString(), generalTraits.ToString(), _scanMode);
    }

    /// <summary>
    ///     Analysis of unique seed.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(SeedData comp, EntityUid target, bool trayChecker)
    {
        var seedName = Loc.GetString(comp.DisplayName);

        var seedChem = "\r\n   ";
        var plantHarvestType = "";
        var exudeGases = "";
        var potency = comp.Potency;
        var yield = comp.Yield;

        var plantSpeciation = "";
        var plantMutations = "";
        var tolerances = "";
        var generalTraits = "";

        if (comp.HarvestRepeat == HarvestType.Repeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-repeat");
        if (comp.HarvestRepeat == HarvestType.NoRepeat) plantHarvestType = Loc.GetString("plant-analyzer-harvest-ephemeral");
        if (comp.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = Loc.GetString("plant-analyzer-harvest-autoharvest");

        seedChem += String.Join("\r\n   ", comp.Chemicals.Select(item => item.Key.ToString()));

        if (comp.ExudeGasses.Count > 0)
        {
            foreach (var (gas, amount) in comp.ExudeGasses)
            {
                exudeGases += "\r\n   " + gas;
            };
        }
        else
        {
            exudeGases = Loc.GetString("plant-analyzer-plant-gasses-No");
        }

        if (_scanMode)
        {
            plantSpeciation += String.Join(", \r\n", comp.MutationPrototypes.Select(item => item.ToString()));
            plantMutations = CheckAllMutations(comp, plantMutations);
            tolerances = CheckAllTolerances(comp, tolerances);
        }

        generalTraits = CheckAllGeneralTraits(comp, generalTraits);

        return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), seedName,
            seedChem, plantHarvestType, exudeGases, potency.ToString(), yield.ToString(), plantMutations, false, plantSpeciation, tolerances.ToString(), generalTraits.ToString(), _scanMode);
    }

    /// <summary>
    ///     Returns information about the seed.
    /// </summary>
    public string CheckAllGeneralTraits(SeedData plant, string generalTraits)
    {
        var name = Loc.GetString(plant.DisplayName);
        var endurance = plant.Endurance;
        var yield = plant.Yield;
        var lifespan = plant.Lifespan;
        var maturation = plant.Maturation;
        var growthStages = plant.GrowthStages;
        var potency = plant.Potency;

        generalTraits = name + ";" + endurance + ";" + yield.ToString() + ";" + lifespan + ";" + maturation + ";" + growthStages + ";" + potency;

        return generalTraits;
    }
    public string CheckAllMutations(SeedData plant, string plantMutations)
    {
        if (plant.TurnIntoKudzu) plantMutations += "\r\n   " + $"{Loc.GetString("plant-analyzer-mutation-turnintokudzu")}";
        if (plant.Seedless) plantMutations += "\r\n   " + $"{Loc.GetString("plant-analyzer-mutation-seedless")}";
        if (plant.Slip) plantMutations += "\r\n   " + $"{Loc.GetString("plant-analyzer-mutation-slip")}";
        if (plant.Sentient) plantMutations += "\r\n   " + $"{Loc.GetString("plant-analyzer-mutation-sentient")}";
        if (plant.Ligneous) plantMutations += "\r\n   " + $"{Loc.GetString("plant-analyzer-mutation-ligneous")}";
        if (plant.Bioluminescent) plantMutations += "\r\n   " + $"{Loc.GetString("plant-analyzer-mutation-bioluminescent")}";
        if (plant.CanScream) plantMutations += "\r\n   " + $"{Loc.GetString("plant-analyzer-mutation-canscream")}";

        return plantMutations;
    }
    public string CheckAllTolerances(SeedData plant, string tolerances)
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

        tolerances = nutrientConsumption + ";" + waterConsumption + ";" + idealHeat + ";" + heatTolerance + ";" + idealLight + ";" + lightTolerance + ";"
                   + toxinsTolerance + ";" + lowPresssureTolerance + ";" + highPressureTolerance + ";" + pestTolerance + ";" + weedTolerance;

        return tolerances;
    }
    private void OnModeSelected(EntityUid uid, PlantAnalyzerComponent component, PlantAnalyzerSetMode args)
    {
        SetMode(uid, component, args.AdvancedScan);
    }
    public void SetMode(EntityUid uid, PlantAnalyzerComponent? component, bool args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (args)
        {
            component.AdvancedScan = true;
        }
        else
        {
            component.AdvancedScan = false;
        }
    }
}
