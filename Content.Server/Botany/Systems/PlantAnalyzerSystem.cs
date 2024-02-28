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
using System.Text;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerSetMode>(OnModeSelected);
    }

    private void OnAfterInteract(Entity<PlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<SeedComponent>(args.Target) && !HasComp<PlantHolderComponent>(args.Target) || !_cell.HasActivatableCharge(ent, user: args.User))
            return;

        if (ent.Comp.DoAfter != null)
            return;

        if (ent.Comp.AdvancedScan)
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.AdvScanDelay, new PlantAnalyzerDoAfterEvent(), ent, target: args.Target, used: ent)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
        }
        else
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.ScanDelay, new PlantAnalyzerDoAfterEvent(), ent, target: args.Target, used: ent)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
        }
    }

    private void OnDoAfter(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;
        if (ent.Comp.AdvancedScan) // Double charge use for advanced scan.
        {
            if (!_cell.TryUseActivatableCharge(ent, user: args.User))
                return;
        }
        if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(ent.Owner, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningEndSound, ent);

        OpenUserInterface(args.User, ent);
        UpdateScannedUser(ent, args.Args.Target.Value);

        args.Handled = true;
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.TryGetUi(analyzer, PlantAnalyzerUiKey.Key, out var ui))
            return;

        _uiSystem.OpenUi(ui, actor.PlayerSession);
    }

    public void UpdateScannedUser(Entity<PlantAnalyzerComponent> ent, EntityUid target)
    {
        if (!_uiSystem.TryGetUi(ent, PlantAnalyzerUiKey.Key, out var ui))
            return;

        TryComp<PlantHolderComponent>(target, out var plantcomp);
        TryComp<SeedComponent>(target, out var seedcomponent);

        if (seedcomponent != null)
        {
            if (seedcomponent.Seed != null) // If unique seed.
            {
                var seedData = seedcomponent.Seed;
                var state = ObtainingGeneDataSeed(seedData, target, false, ent.Comp.AdvancedScan);
                _uiSystem.SendUiMessage(ui, state);
            }
            else if (seedcomponent.SeedId != null && _prototypeManager.TryIndex(seedcomponent.SeedId, out SeedPrototype? protoSeed)) // Get the seed protoype.
            {
                var seedProtoId = protoSeed;
                var state = ObtainingGeneDataSeedProt(protoSeed, target, ent.Comp.AdvancedScan);
                _uiSystem.SendUiMessage(ui, state);
            }
        }
        else if (plantcomp != null) // If we poke the plantholder, it checks the plantholder seed.
        {
            var seedData = plantcomp.Seed;
            if (seedData != null)
            {
                var state = ObtainingGeneDataSeed(seedData, target, true, ent.Comp.AdvancedScan);  // SeedData is a unique seed in a tray.
                _uiSystem.SendUiMessage(ui, state);
            }
        }
    }

    /// <summary>
    ///     Analysis of seed from prototype.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeedProt(SeedPrototype seedProto, EntityUid target, Boolean scanMode)
    {
        string plantHarvestType = "";
        if (seedProto.HarvestRepeat == HarvestType.Repeat) plantHarvestType = HarvestType.Repeat.ToString();
        if (seedProto.HarvestRepeat == HarvestType.NoRepeat) plantHarvestType = HarvestType.NoRepeat.ToString();
        if (seedProto.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = HarvestType.SelfHarvest.ToString();

        string exudeGases = new StringBuilder("").AppendJoin("\n   ", seedProto.ExudeGasses.Select(item => item.Key.ToString())).ToString();
        string seedChem = new StringBuilder("\n   ").AppendJoin("\n   ", seedProto.Chemicals.Select(item => item.Key.ToString())).ToString();
        string speciation = new StringBuilder("").AppendJoin("\n   ", seedProto.MutationPrototypes.Select(item => item.ToString())).ToString();
        string traits = new StringBuilder("\n   ").AppendJoin("\n   ", CheckAllMutations(seedProto).Select(item => item.ToString())).ToString();

        return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), scanMode, false,
            Loc.GetString(seedProto.DisplayName), seedChem, plantHarvestType, exudeGases, seedProto.Endurance,
            seedProto.Yield, seedProto.Lifespan, seedProto.Maturation, seedProto.GrowthStages, seedProto.Potency,
            seedProto.NutrientConsumption, seedProto.WaterConsumption, seedProto.IdealHeat, seedProto.HeatTolerance,
            seedProto.IdealLight, seedProto.LightTolerance, seedProto.ToxinsTolerance, seedProto.LowPressureTolerance,
            seedProto.HighPressureTolerance, seedProto.PestTolerance, seedProto.WeedTolerance, traits, seedProto.MutationPrototypes);
    }

    /// <summary>
    ///     Analysis of unique seed.
    /// </summary>
    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(SeedData seed, EntityUid target, bool trayChecker, Boolean scanMode)
    {
        string plantHarvestType = "";
        if (seed.HarvestRepeat == HarvestType.Repeat) plantHarvestType = HarvestType.Repeat.ToString();
        if (seed.HarvestRepeat == HarvestType.NoRepeat) plantHarvestType = HarvestType.NoRepeat.ToString();
        if (seed.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = HarvestType.SelfHarvest.ToString();

        string exudeGases = new StringBuilder("").AppendJoin("\n   ", seed.ExudeGasses.Select(item => item.Key.ToString())).ToString();
        string seedChem = new StringBuilder("\n   ").AppendJoin("\n   ", seed.Chemicals.Select(item => item.Key.ToString())).ToString();
        string speciation = new StringBuilder("").AppendJoin("\n   ", seed.MutationPrototypes.Select(item => item.ToString())).ToString();
        string traits = new StringBuilder("\n   ").AppendJoin("\n   ", CheckAllMutations(seed).Select(item => item.ToString())).ToString();

        return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), scanMode, trayChecker,
            Loc.GetString(seed.DisplayName), seedChem, plantHarvestType, exudeGases, seed.Endurance,
            seed.Yield, seed.Lifespan, seed.Maturation, seed.GrowthStages, seed.Potency,
            seed.NutrientConsumption, seed.WaterConsumption, seed.IdealHeat, seed.HeatTolerance,
            seed.IdealLight, seed.LightTolerance, seed.ToxinsTolerance, seed.LowPressureTolerance,
            seed.HighPressureTolerance, seed.PestTolerance, seed.WeedTolerance, traits, seed.MutationPrototypes);
    }

    /// <summary>
    ///     Returns information about the seeds boolean mutations.
    /// </summary>
    public List<string> CheckAllMutations(SeedData plant)
    {
        List<string> mutationsList = new List<string>();
        if (plant.TurnIntoKudzu) mutationsList.Add(Loc.GetString("plant-analyzer-mutation-turnintokudzu"));
        if (plant.Seedless) mutationsList.Add(Loc.GetString("plant-analyzer-mutation-seedless"));
        if (plant.Slip) mutationsList.Add(Loc.GetString("plant-analyzer-mutation-slip"));
        if (plant.Sentient) mutationsList.Add(Loc.GetString("plant-analyzer-mutation-sentient"));
        if (plant.Ligneous) mutationsList.Add(Loc.GetString("plant-analyzer-mutation-ligneous"));
        if (plant.Bioluminescent) mutationsList.Add(Loc.GetString("plant-analyzer-mutation-bioluminescent"));
        if (plant.CanScream) mutationsList.Add(Loc.GetString("plant-analyzer-mutation-canscream"));

        return mutationsList;
    }

    private void OnModeSelected(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerSetMode args)
    {
        SetMode(ent, args.AdvancedScan);
    }

    public void SetMode(Entity<PlantAnalyzerComponent> ent, bool isAdvMode)
    {
        if (ent.Comp.DoAfter != null)
            return;
        ent.Comp.AdvancedScan = isAdvMode;
    }
}
