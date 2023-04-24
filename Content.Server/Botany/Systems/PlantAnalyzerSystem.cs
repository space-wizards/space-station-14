using System.Diagnostics;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Botany.PlantAnalyzer;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;



namespace Content.Server.Botany.Systems;

public sealed class SeedScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    private  PlantHolderComponent _plantHolder = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, ActivateInWorldEvent>(HandleActivateInWorld);
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
    }

    private void HandleActivateInWorld(EntityUid uid, PlantAnalyzerComponent plantAnalyzer, ActivateInWorldEvent args)
    {
        OpenUserInterface(args.User, plantAnalyzer);
    }
    private void OnAfterInteract(EntityUid uid, PlantAnalyzerComponent plantAnalyzer, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || (!HasComp<SeedComponent>(args.Target) && !HasComp<PlantHolderComponent>(args.Target)))
            return;
        // _audio.PlayPvs(healthAnalyzer.ScanningBeginSound, uid);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(args.User, plantAnalyzer.ScanDelay, new PlantAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true
        });
    }


    private void OnDoAfter(EntityUid uid, PlantAnalyzerComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;
        // _audio.PlayPvs(component.ScanningEndSound, args.Args.User);

        UpdateScannedUser(uid, args.Args.User, args.Args.Target.Value, component);



        args.Handled = true;
    }







    private void OpenUserInterface(EntityUid user, PlantAnalyzerComponent plantAnalyzer)
    {
        if (!TryComp<ActorComponent>(user, out var actor) || plantAnalyzer.UserInterface == null)
            return;

        _uiSystem.OpenUi(plantAnalyzer.UserInterface ,actor.PlayerSession);
    }


    public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid target, PlantAnalyzerComponent? plantAnalyzer)
    {

        if (!Resolve(uid, ref plantAnalyzer)) return;

        if (target == null || plantAnalyzer.UserInterface == null) return;

        if (!TryComp<SeedComponent>(target, out var component) & !TryComp<PlantHolderComponent>(target, out var comp)) return;

        if (comp is { Seed: null })
            return;

        OpenUserInterface(user, plantAnalyzer);

        var state = default(PlantAnalyzerScannedSeedPlantInformation);
        var seedDat = default(SeedData);

        if (component != null)    //where check seed
        {


            if (component?.SeedId != null)
            {
                var seedId=  PrototypeManager.Index<SeedPrototype>(component.SeedId);
                state = ObtainingGeneDataSeedProt(seedId, target);

            }
            else if (component?.Seed != null)
            {
                seedDat = component.Seed;
                state = ObtainingGeneDataSeed(seedDat, target, false);

            }

        }
        else if (comp !=null)    //where check if we poke the plant, it checks the plant
        {
            _plantHolder = comp;
            seedDat = comp.Seed;

            if (seedDat != null)
            {
                state = ObtainingGeneDataSeed(seedDat, target, true);

            }


        }
        if(state != null)
            _uiSystem.TrySetUiState(uid, PlantAnalyzerUiKey.Key, state);


    }

    /// <summary>
    ///Analysis of seed from prototip
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeedProt(SeedPrototype comp, EntityUid target )
    {
        string Chem = "";
        string plantYeild= "";
        string plantPotency = "";
        string plantHarvestType;
        string plantMut = "";

        float plantMinTemp;
        float plantMaxTemp;
        float plantEndurance;

        var plantString = comp.Name;
        var seedYeild = comp.Yield;
        var seedPotency = comp.Potency;


          if (comp.HarvestRepeat == HarvestType.Repeat)
              plantHarvestType = "Repeat";
          else
              plantHarvestType = "No Repeat";


          Chem += String.Join(", ", comp.Chemicals.Select(item => item.Key.ToString()));

          plantMinTemp = 283f;
          plantMaxTemp =  303f;

          plantEndurance = comp.Endurance;



          return new PlantAnalyzerScannedSeedPlantInformation(target, plantEndurance, seedYeild.ToString(), seedPotency.ToString(),
              plantHarvestType, Chem,plantMinTemp ,plantMaxTemp, plantMut, plantString, 0f, " ", false );
        }



    /// <summary>
    ///Analysis of seed/plant characteristics occurs here .
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(SeedData comp, EntityUid target, bool trayChecker )    //analyze seed from hydroponic
    {
        string Chem = "";
        string plantYeild= "";
        string plantPotency = "";
        string plantHarvestType;
        string plantMut = "";
        string plantProblems = "";

        float plantHealth = 0f;

        var yeild = comp.Yield;
        var potency = comp.Potency;
        var plantMinTemp = comp.IdealHeat - comp.HeatTolerance;
        var plantMaxTemp = comp.IdealHeat + comp.HeatTolerance;
        var plantEndurance = comp.Endurance;
        var plantString = comp.Name;


        plantYeild = yeild.ToString();
        plantPotency = potency.ToString();
        if (comp.HarvestRepeat == HarvestType.Repeat) plantHarvestType = "Repeat"; else plantHarvestType = "No Repeat";
        Chem += String.Join(", ", comp.Chemicals.Select(item => item.Key.ToString()));

        plantMut= CheckAllMutation(comp, plantMut);

        if (trayChecker)
        {
            plantHealth = _plantHolder.Health;
            plantProblems = CheckAllProblems(_plantHolder, plantProblems, target);
        }


        return new PlantAnalyzerScannedSeedPlantInformation(target, plantEndurance, plantYeild, plantPotency, plantHarvestType, Chem,
            plantMinTemp, plantMaxTemp, plantMut, plantString,  plantHealth,  plantProblems, trayChecker );
    }









    public string CheckAllMutation(SeedData plant,  string plantMut)
    {
        if (plant.Viable == false) plantMut += $"{Loc.GetString("plant-analyzer-mutation-unviable")}"+ ", ";
        if (plant.TurnIntoKudzu) plantMut += $"{Loc.GetString("plant-analyzer-mutation-turnintokudzu")}"+ ", ";
        if (plant.Seedless) plantMut += $"{Loc.GetString("plant-analyzer-mutation-seedless")}"+ ", ";
        if (plant.Slip) plantMut += $"{Loc.GetString("plant-analyzer-mutation-slip")}"+ ", ";
        if (plant.Sentient) plantMut += $"{Loc.GetString("plant-analyzer-mutation-sentient")}"+ ", ";
        if (plant.Ligneous) plantMut += $"{Loc.GetString("plant-analyzer-mutation-ligneous")}"+ ", ";
        if (plant.Bioluminescent) plantMut += $"{Loc.GetString("plant-analyzer-mutation-bioluminescent")}"+ ", ";
        if (plant.CanScream) plantMut += $"{Loc.GetString("plant-analyzer-mutation-canscream")}"+ ", ";
        plantMut = plantMut.TrimEnd(',', ' ');
        return plantMut;
    }

    public string CheckAllProblems(PlantHolderComponent plant, string plantProblems, EntityUid target)
    {
        var environment = _atmosphere.GetContainingMixture(target, true, true) ?? GasMixture.SpaceGas;
        var pressure = environment.Pressure;

        if (plant.Seed == null) return plantProblems;


        if (plant.WaterLevel < 10) plantProblems += $"{Loc.GetString("plant-analyzer-problems-water")}"+ ", ";
        if (plant.Toxins > 0) plantProblems += $"{Loc.GetString("plant-analyzer-problems-toxins")}"+ ", ";
        if (MathF.Abs(environment.Temperature - plant.Seed.IdealHeat) > plant.Seed.HeatTolerance)
            plantProblems+=  $"{Loc.GetString("plant-analyzer-problems-temperature")}"+ ", ";
        if (pressure < plant.Seed.LowPressureTolerance || pressure > plant.Seed.HighPressureTolerance)
            plantProblems+=  $"{Loc.GetString("plant-analyzer-problems-pressure")}"+ ", ";
        if( plant.PestLevel > 0) plantProblems+=  $"{Loc.GetString("plant-analyzer-problems-pests")}"+ ", ";
        if (plant.Age > plant.Seed.Lifespan)
            plantProblems+=  $"{Loc.GetString("plant-analyzer-problems-age")}"+ ", ";

        plantProblems = plantProblems.TrimEnd(',', ' ');
        return plantProblems;
    }

}
