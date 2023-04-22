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

        if (!TryComp<SeedComponent>(target, out var component) ) return;


        OpenUserInterface(user, plantAnalyzer);

      var state = ObtainingGeneData(component, target);

        _uiSystem.TrySetUiState(uid, PlantAnalyzerUiKey.Key, state);
    }

    /// <summary>
    ///Analysis of seed/plant characteristics occurs here .
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneData(SeedComponent comp, EntityUid target )
    {
        string Chem = "";
        string plantYeild= "";
        string plantPotency = "";
        string plantHarvestType;
        string plantMut = "";

        var environment = _atmosphere.GetContainingMixture(target, true, true) ?? GasMixture.SpaceGas;
        float plantMinTemp;
        float plantMaxTemp;

        float plantEndurance;

        if (comp?.Seed == null && comp?.SeedId != null )
        {
            //analyz seed from prototipe
            var seedId=  PrototypeManager.Index<SeedPrototype>(comp.SeedId);

          var seedYeild = seedId.Yield;
          var seedPotency = seedId.Potency;
          if (seedId.HarvestRepeat == HarvestType.Repeat)
              plantHarvestType = "Repeat";
          else
              plantHarvestType = "No Repeat";


          Chem += String.Join(", ", seedId.Chemicals.Select(item => item.Key.ToString()));

          plantMinTemp = 283f;
          plantMaxTemp =  303f;

          plantEndurance = seedId.Endurance;


          return new PlantAnalyzerScannedSeedPlantInformation(target,plantEndurance, seedYeild.ToString(), seedPotency.ToString(),plantHarvestType, Chem,plantMinTemp ,plantMaxTemp, plantMut);
        }
        else
        {
            //analyz seed from hydroponic

            var Yeild = comp?.Seed?.Yield;
            var Potency = comp?.Seed?.Potency;

            plantYeild = Yeild?.ToString() ?? "0";
            plantPotency = Potency?.ToString() ?? "0";

            if (comp?.Seed?.HarvestRepeat == HarvestType.Repeat) plantHarvestType = "Repeat"; else plantHarvestType = "No Repeat";
            if (comp?.Seed != null) Chem += String.Join(", ", comp.Seed.Chemicals.Select(item => item.Key.ToString()));

             plantMinTemp = comp?.Seed?.IdealHeat - comp?.Seed?.HeatTolerance ?? 0;
             plantMaxTemp =  comp?.Seed?.IdealHeat + comp?.Seed?.HeatTolerance ?? 0;

             plantEndurance = comp?.Seed?.Endurance ?? 0;

          if(comp!= null) plantMut= CheckAllMutation(comp, plantMut);

            return new PlantAnalyzerScannedSeedPlantInformation(target, plantEndurance, plantYeild, plantPotency, plantHarvestType, Chem, plantMinTemp, plantMaxTemp, plantMut );
        }
    }

    public string CheckAllMutation(SeedComponent comp,  string plantMut)
    {
      var Plant = comp.Seed;
      if (Plant?.Viable == false) plantMut += $"{Loc.GetString("plant-analyzer-mutation-unviable")}"+ ", ";
      if (Plant?.TurnIntoKudzu == true) plantMut += $"{Loc.GetString("plant-analyzer-mutation-turnintokudzu")}"+ ", ";
      if (Plant?.Seedless == true) plantMut += $"{Loc.GetString("plant-analyzer-mutation-seedless")}"+ ", ";
      if (Plant?.Slip == true) plantMut += $"{Loc.GetString("plant-analyzer-mutation-slip")}"+ ", ";
      if (Plant?.Sentient == true) plantMut += $"{Loc.GetString("plant-analyzer-mutation-sentient")}"+ ", ";
      if (Plant?.Ligneous == true) plantMut += $"{Loc.GetString("plant-analyzer-mutation-ligneous")}"+ ", ";
      if (Plant?.Bioluminescent == true) plantMut += $"{Loc.GetString("plant-analyzer-mutation-bioluminescent")}"+ ", ";
      if (Plant?.CanScream == true) plantMut += $"{Loc.GetString("plant-analyzer-mutation-canscream")}"+ ", ";
      plantMut = plantMut.TrimEnd(',', ' ');
      return plantMut;

    }

}
