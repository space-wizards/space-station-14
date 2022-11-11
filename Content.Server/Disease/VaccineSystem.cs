using Content.Shared.Disease.Components;
using Content.Shared.Materials;
using Content.Shared.Research.Components;
using Content.Shared.Disease;
using Content.Server.Disease.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Research;
using Content.Server.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Disease
{
    public sealed class VaccineSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseDiagnosisSystem _diseaseDiagnosisSystem = default!;
        [Dependency] private readonly SharedMaterialStorageSystem _storageSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly ResearchSystem _research = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, CreateVaccineMessage>(OnCreateVaccineMessageReceived);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, DiseaseMachineFinishedEvent>(OnVaccinatorFinished);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, MaterialAmountChangedEvent>(OnVaccinatorAmountChanged);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, ResearchClientServerSelectedMessage>(OnServerSelected);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, VaccinatorServerSelectionMessage>(OpenServerList);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
        }

        /// <summary>
        /// Creates a vaccine, if possible, when sent a UI message to do so.
        /// </summary>
        private void OnCreateVaccineMessageReceived(EntityUid uid, DiseaseVaccineCreatorComponent component, CreateVaccineMessage args)
        {
            if (HasComp<DiseaseMachineRunningComponent>(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (_storageSystem.GetMaterialAmount(uid, "Biomass") < component.BiomassCost)
                return;

            var machine = Comp<DiseaseMachineComponent>(uid);
            machine.Disease = args.Disease;

            _diseaseDiagnosisSystem.AddQueue.Enqueue(uid);
            _diseaseDiagnosisSystem.UpdateAppearance(uid, true, true);
            SoundSystem.Play("/Audio/Machines/vaccinator_running.ogg", Filter.Pvs(uid), uid);
        }

        /// <summary>
        /// Prints a vaccine that will vaccinate
        /// against the disease on the inserted swab.
        /// </summary>
        private void OnVaccinatorFinished(EntityUid uid, DiseaseVaccineCreatorComponent component, DiseaseMachineFinishedEvent args)
        {
            _diseaseDiagnosisSystem.UpdateAppearance(uid, this.IsPowered(uid, EntityManager), false);

            if (!_storageSystem.TryChangeMaterialAmount(uid, "Biomass", (0 - component.BiomassCost)))
                return;

            UpdateUserInterfaceState(uid, component);
            // spawn a vaccine
            var vaxx = Spawn(args.Machine.MachineOutput, Transform(uid).Coordinates);

            if (args.Machine.Disease == null)
                return;

            MetaData(vaxx).EntityName = Loc.GetString("vaccine-name", ("disease", args.Machine.Disease.Name));
            MetaData(vaxx).EntityDescription = Loc.GetString("vaccine-desc", ("disease", args.Machine.Disease.Name));

            if (!TryComp<DiseaseVaccineComponent>(vaxx, out var vaxxComp))
                return;

            vaxxComp.Disease = args.Machine.Disease;
        }

        private void OnVaccinatorAmountChanged(EntityUid uid, DiseaseVaccineCreatorComponent component, MaterialAmountChangedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        private void OnServerSelected(EntityUid uid, DiseaseVaccineCreatorComponent component, ResearchClientServerSelectedMessage args)
        {
            var server = _research.GetServerById(args.ServerId);

            if (server == null)
                return;

            if (!TryComp<DiseaseServerComponent>(server.Owner, out var diseaseServer))
                return;

            component.DiseaseServer = diseaseServer;
            UpdateUserInterfaceState(uid, component);
        }

        private void OpenServerList(EntityUid uid, DiseaseVaccineCreatorComponent component, VaccinatorServerSelectionMessage args)
        {
            _uiSys.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
        }

        private void AfterUIOpen(EntityUid uid, DiseaseVaccineCreatorComponent component, AfterActivatableUIOpenEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        public void UpdateUserInterfaceState(EntityUid uid, DiseaseVaccineCreatorComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = _uiSys.GetUi(uid, VaccineMachineUiKey.Key);
            var biomass = _storageSystem.GetMaterialAmount(uid, "Biomass");

            Logger.Error("Passing " + component.DiseaseServer?.Diseases.Count + " diseases.");

            var state = new VaccineMachineUpdateState(biomass, component.DiseaseServer?.Diseases ?? new List<DiseasePrototype>());
            _uiSys.SetUiState(ui, state);
        }
    }
}
