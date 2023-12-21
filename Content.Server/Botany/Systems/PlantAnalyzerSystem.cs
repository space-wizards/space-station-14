using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.PlantAnalyzer;
using Content.Shared.Botany;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.PowerCell;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Botany.Systems
{
    public sealed class PlantAnalyzerSystem : EntitySystem
    {
        // [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private PlantHolderComponent _plantHolder = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        }

        private void OnAfterInteract(EntityUid uid, PlantAnalyzerComponent plantAnalyzer, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || !HasComp<SeedComponent>(args.Target) && !HasComp<PlantHolderComponent>(args.Target))
                return;
            //_audio.PlayPvs(plantAnalyzer.ScanningBeginSound, uid);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, plantAnalyzer.ScanDelay, new PlantAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
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

            //_audio.PlayPvs(component.ScanningEndSound, args.Args.User);

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

            //OpenUserInterface(user, uid);
            /*
            if (!TryComp<SeedComponent>(target, out var seedcomponent))
                return;
            if (seedcomponent is { Seed: null })
                return;
            */
            TryComp<PlantHolderComponent>(target, out var plantcomp);
            TryComp<SeedComponent>(target, out var seedcomponent);

            OpenUserInterface(user, uid);
            var state = default(PlantAnalyzerScannedSeedPlantInformation);
            var seedData = default(SeedData);
            var seedProtoId = default(SeedPrototype);


            if (seedcomponent != null)
            {
                if (seedcomponent.SeedId != null && _prototypeManager.TryIndex(seedcomponent.SeedId, out SeedPrototype? protoSeed)) //wir haben eine SeedId, also ziehen wir den prototypen
                {
                    seedProtoId = protoSeed;
                    state = ObtainingGeneDataSeedProt(protoSeed, target);
                }
                else if (seedcomponent.Seed != null) // unqiue seed
                {
                    seedData = seedcomponent.Seed;
                    state = ObtainingGeneDataSeed(seedData, target, false);
                }
            }
            else if (plantcomp != null)    //where check if we poke the plantholder, it checks the plantholder seed
            {
                _plantHolder = plantcomp;
                seedData = plantcomp.Seed;
                if (seedData != null)
                {
                    state = ObtainingGeneDataSeed(seedData, target, true); //seedData is a unique seed
                }
            }

            if (state == null)
                return;

            _uiSystem.SendUiMessage(ui, state);

        }
        public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeedProt(SeedPrototype comp, EntityUid target)
        {   //for non unique seedpacket
            string seedChem = "";
            string plantYeild = "";
            string plantPotency = "";
            string plantHarvestType;
            string plantMutations = "";

            string exudeGases = "";

            //var plantString = comp.Name;
            //var seedName = comp.Name;
            var seedName = Loc.GetString(comp.DisplayName);
            var seedYield = comp.Yield;
            var seedPotency = comp.Potency;

            if (comp.HarvestRepeat == HarvestType.Repeat)
                plantHarvestType = "Repeat";
            else
                plantHarvestType = "No Repeat";
            if (comp.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = "Auto";

            seedChem += "\r\n";
            seedChem += String.Join(", ", comp.Chemicals.Select(item => item.Key.ToString()));

            if (comp.ExudeGasses.Count > 0)
            {
                exudeGases = comp.ExudeGasses.Values.First().ToString();
                exudeGases = "Yes";
            }
            else
            {
                exudeGases = "No";
            }
            plantMutations = CheckAllMutation(comp, plantMutations);
            return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), seedName, seedYield.ToString(), seedPotency.ToString(), seedChem, plantHarvestType, exudeGases, plantMutations);
        }

        public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(SeedData comp, EntityUid target, bool trayChecker)
        {   //analyze seed from hydroponic
            string seedChem = "";
            string plantYield = "";
            string plantPotency = "";
            string plantHarvestType;
            string plantMutations = "";
            string exudeGases = "";

            var yield = comp.Yield;
            var potency = comp.Potency;

            //var seedName = comp.Name;
            var seedName = Loc.GetString(comp.DisplayName);

            plantYield = yield.ToString();
            plantPotency = potency.ToString(); //self harvest type
            if (comp.HarvestRepeat == HarvestType.Repeat) plantHarvestType = "Multi"; else plantHarvestType = "Single";
            if (comp.HarvestRepeat == HarvestType.SelfHarvest) plantHarvestType = "Auto";
            seedChem += "\r\n";
            seedChem += String.Join(", \r\n", comp.Chemicals.Select(item => item.Key.ToString()));

            if (comp.ExudeGasses.Count > 0)
            {
                exudeGases = "Yes";
                exudeGases = comp.ExudeGasses.Values.First().ToString();
                //exudeGases = comp.ExudeGasses.Keys.ToString();
            }
            else
            {
                exudeGases = "No";
            }
            plantMutations = CheckAllMutation(comp, plantMutations);
            return new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), seedName, plantYield.ToString(), plantPotency.ToString(), seedChem, plantHarvestType, exudeGases, plantMutations);
        }

        public string CheckAllMutation(SeedData plant, string plantMutations)
        {
            String plantMut = "test";

            //plantMut += String.Join(", \r\n", plant.MutationPrototypes.Select(item => item.ToString())); possible speciiation

            if (plant.Viable == false) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-unviable")}" + ", \r\n";
            if (plant.TurnIntoKudzu) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-turnintokudzu")}" + ", \r\n";
            if (plant.Seedless) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-seedless")}" + ", \r\n";
            if (plant.Slip) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-slip")}" + ", \r\n";
            if (plant.Sentient) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-sentient")}" + ", \r\n";
            if (plant.Ligneous) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-ligneous")}" + ", \r\n";
            if (plant.Bioluminescent) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-bioluminescent")}" + ", \r\n";
            if (plant.CanScream) plantMutations += $"{Loc.GetString("plant-analyzer-mutation-canscream")}" + ", ";
            plantMutations = plantMutations.TrimEnd(',', ' ');

            return plantMutations;
        }


    }
}
