using Content.Shared.Disease.Components;
using Content.Shared.Materials;
using Content.Server.Disease.Components;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Server.GameObjects;

namespace Content.Server.Disease
{
    public sealed class VaccineSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseDiagnosisSystem _diseaseDiagnosisSystem = default!;
        [Dependency] private readonly SharedMaterialStorageSystem _storageSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, CreateVaccineMessage>(OnCreateVaccineMessageReceived);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, DiseaseMachineFinishedEvent>(OnVaccinatorFinished);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, MaterialAmountChangedEvent>(OnVaccinatorAmountChanged);
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

        public void UpdateUserInterfaceState(EntityUid uid, DiseaseVaccineCreatorComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = _uiSys.GetUi(uid, VaccineMachineUiKey.Key);
            var biomass = _storageSystem.GetMaterialAmount(uid, "Biomass");

            var state = new VaccineMachineUpdateState(biomass);
            _uiSys.SetUiState(ui, state);
        }
    }
}
