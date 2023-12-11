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
            //    if (!HasComp<SeedComponent>(target) || !HasComp<PlantHolderComponent>(target))
            //        return;

            if (!TryComp<SeedComponent>(target, out var seedcomponent) && !TryComp<PlantHolderComponent>(target, out var plantcomp))
                return;

            if (seedcomponent is { Seed: null })
                return;

            var state = default(PlantAnalyzerScannedSeedPlantInformation);
            var seedDat = default(SeedData);

            if (seedcomponent != null)
            {
                if (seedcomponent?.SeedId != null)
                {
                    var seedId = PrototypeManager.Index<SeedPrototype>(seedcomponent.SeedId);
                    state = ObtainingGeneDataSeedProt(seedId, target);
                }
                else if (seedcomponent?.Seed != null)
                {
                    seedDat = seedcomponent.Seed;
                    state = ObtainingGeneDataSeed(seedDat, target, false);

                }
            }
            else if (plantcomp != null)    //where check if we poke the plant, it checks the plant
            {
                _plantHolder = plantcomp;
                seedDat = plantcomp.Seed;

                if (seedDat != null)
                {
                    state = ObtainingGeneDataSeed(seedDat, target, true);

                }
            }

            if (state != null)
                _uiSystem.TrySetUiState(uid, PlantAnalyzerUiKey.Key, state);

            _uiSystem.SendUiMessage(ui, new PlantAnalyzerScannedSeedPlantInformation(GetNetEntity(target), "hallo", "wie", "gehts?", "seedchem?"));

            OpenUserInterface(user, uid);
        }
        public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeedProt(SeedPrototype comp, EntityUid target)
        { //for seedpacket?
            string Chem = "";
            string plantYeild = "";
            string plantPotency = "";
            string plantHarvestType;
            string plantMut = "";

            float plantMinTemp;
            float plantMaxTemp;
            float plantEndurance;

            var plantString = comp.Name;
            var seedYield = comp.Yield;
            var seedPotency = comp.Potency;

            if (comp.HarvestRepeat == HarvestType.Repeat)
                plantHarvestType = "Repeat";
            else
                plantHarvestType = "No Repeat";

            Chem += String.Join(", ", comp.Chemicals.Select(item => item.Key.ToString()));

            plantMinTemp = 283f;
            plantMaxTemp = 303f;

            plantEndurance = comp.Endurance;

            return new PlantAnalyzerScannedSeedPlantInformation(target, plantString, seedYield.ToString(), seedPotency.ToString(), Chem);
        }

        public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(SeedData comp, EntityUid target, bool trayChecker)    //analyze seed from hydroponic
        {
            string Chem = "";
            string plantYield = "";
            string plantPotency = "";
            string plantHarvestType;
            string plantMut = "";
            string plantProblems = "";

            float plantHealth = 0f;

            var yield = comp.Yield;
            var potency = comp.Potency;
            var plantMinTemp = comp.IdealHeat - comp.HeatTolerance;
            var plantMaxTemp = comp.IdealHeat + comp.HeatTolerance;
            var plantEndurance = comp.Endurance;
            var plantString = comp.Name;


            plantYield = yield.ToString();
            plantPotency = potency.ToString();
            if (comp.HarvestRepeat == HarvestType.Repeat) plantHarvestType = "Repeat"; else plantHarvestType = "No Repeat";
            Chem += String.Join(", ", comp.Chemicals.Select(item => item.Key.ToString()));


            return new PlantAnalyzerScannedSeedPlantInformation(target, plantString, plantYield.ToString(), plantPotency.ToString(), Chem);
        }
    }
}
