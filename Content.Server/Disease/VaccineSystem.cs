using System.Threading;
using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Content.Shared.Materials;
using Content.Shared.Research.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Content.Server.Disease.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Research;
using Content.Server.UserInterface;
using Content.Server.Construction;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, CreateVaccineMessage>(OnCreateVaccineMessageReceived);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, DiseaseMachineFinishedEvent>(OnVaccinatorFinished);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, MaterialAmountChangedEvent>(OnVaccinatorAmountChanged);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, ResearchClientServerSelectedMessage>(OnServerSelected);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, VaccinatorSyncRequestMessage>(OnSyncRequest);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, VaccinatorServerSelectionMessage>(OpenServerList);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, UpgradeExamineEvent>(OnUpgradeExamine);

            /// vaccines, the item
            SubscribeLocalEvent<DiseaseVaccineComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DiseaseVaccineComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<TargetVaxxSuccessfulEvent>(OnTargetVaxxSuccessful);
            SubscribeLocalEvent<VaxxCancelledEvent>(OnVaxxCancelled);

        }

        /// <summary>
        /// Creates a vaccine, if possible, when sent a UI message to do so.
        /// </summary>
        private void OnCreateVaccineMessageReceived(EntityUid uid, DiseaseVaccineCreatorComponent component, CreateVaccineMessage args)
        {
            if (HasComp<DiseaseMachineRunningComponent>(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (_storageSystem.GetMaterialAmount(uid, "Biomass") < component.BiomassCost * args.Amount)
                return;

            if (!_prototypeManager.TryIndex<DiseasePrototype>(args.Disease, out var disease))
                return;

            if (!disease.Infectious)
                return;

            component.Queued = args.Amount;
            QueueNext(uid, component, disease);
            UpdateUserInterfaceState(uid, component, true);
        }

        private void QueueNext(EntityUid uid, DiseaseVaccineCreatorComponent component, DiseasePrototype disease, DiseaseMachineComponent? machine = null)
        {
            if (!Resolve(uid, ref machine))
                return;

            machine.Disease = disease;
            _diseaseDiagnosisSystem.AddQueue.Enqueue(uid);
            _diseaseDiagnosisSystem.UpdateAppearance(uid, true, true);
            _audioSystem.PlayPvs(component.RunningSoundPath, uid);
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

            // spawn a vaccine
            var vaxx = Spawn(args.Machine.MachineOutput, Transform(uid).Coordinates);

            if (args.Machine.Disease == null)
                return;

            MetaData(vaxx).EntityName = Loc.GetString("vaccine-name", ("disease", args.Machine.Disease.Name));
            MetaData(vaxx).EntityDescription = Loc.GetString("vaccine-desc", ("disease", args.Machine.Disease.Name));

            if (!TryComp<DiseaseVaccineComponent>(vaxx, out var vaxxComp))
                return;

            vaxxComp.Disease = args.Machine.Disease;

            component.Queued--;
            if (component.Queued > 0)
            {
                args.Dequeue = false;
                QueueNext(uid, component, args.Machine.Disease, args.Machine);
                UpdateUserInterfaceState(uid, component);
            }
            else
            {
                UpdateUserInterfaceState(uid, component, false);
            }
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

        private void OnSyncRequest(EntityUid uid, DiseaseVaccineCreatorComponent component, VaccinatorSyncRequestMessage args)
        {
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

        private void OnRefreshParts(EntityUid uid, DiseaseVaccineCreatorComponent component, RefreshPartsEvent args)
        {
            int costRating = (int) args.PartRatings[component.MachinePartCost];

            component.BiomassCost = component.BaseBiomassCost - costRating;
            UpdateUserInterfaceState(uid, component);
        }

        private void OnUpgradeExamine(EntityUid uid, DiseaseVaccineCreatorComponent component, UpgradeExamineEvent args)
        {
            args.AddNumberUpgrade("vaccine-machine-cost-upgrade", component.BiomassCost - component.BaseBiomassCost);
        }

        public void UpdateUserInterfaceState(EntityUid uid, DiseaseVaccineCreatorComponent? component = null, bool? overrideLocked = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = _uiSys.GetUi(uid, VaccineMachineUiKey.Key);
            var biomass = _storageSystem.GetMaterialAmount(uid, "Biomass");

            var diseases = new List<(string id, string name)>();

            if (component.DiseaseServer != null)
            {
                foreach (var disease in component.DiseaseServer.Diseases)
                {
                    if (!disease.Infectious)
                        continue;

                    diseases.Add((disease.ID, disease.Name));
                }
            }
            var state = new VaccineMachineUpdateState(biomass, component.BiomassCost, diseases, overrideLocked ?? HasComp<DiseaseMachineRunningComponent>(uid));
            _uiSys.SetUiState(ui, state);
        }



        /// <summary>
        /// Called when a vaccine is used on someone
        /// to handle the vaccination doafter
        /// </summary>
        private void OnAfterInteract(EntityUid uid, DiseaseVaccineComponent vaxx, AfterInteractEvent args)
        {
            if (vaxx.CancelToken != null)
            {
                vaxx.CancelToken.Cancel();
                vaxx.CancelToken = null;
                return;
            }
            if (args.Target == null)
                return;

            if (!args.CanReach)
                return;

            if (vaxx.CancelToken != null)
                return;

            if (!TryComp<DiseaseCarrierComponent>(args.Target, out var carrier))
                return;

            if (vaxx.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("vaxx-already-used"), args.User, Filter.Entities(args.User));
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("vaccine-inject-start-agent", ("target", args.Target), ("vaccine", args.Used)), args.Target.Value, Filter.Entities(args.User));
            _popupSystem.PopupEntity(Loc.GetString("vaccine-inject-start-patient", ("user", args.User), ("vaccine", args.Used)), args.Target.Value, Filter.Entities(args.Target.Value), Shared.Popups.PopupType.SmallCaution);

            vaxx.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, vaxx.InjectDelay, vaxx.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetVaxxSuccessfulEvent(args.User, args.Target, vaxx, carrier),
                BroadcastCancelledEvent = new VaxxCancelledEvent(vaxx),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        /// <summary>
        /// Called when a vaccine is examined.
        /// Currently doesn't do much because
        /// vaccines don't have unique art with a seperate
        /// state visualizer.
        /// </summary>
        private void OnExamined(EntityUid uid, DiseaseVaccineComponent vaxx, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (vaxx.Used)
                    args.PushMarkup(Loc.GetString("vaxx-used"));
                else
                    args.PushMarkup(Loc.GetString("vaxx-unused"));
            }
        }

        /// <summary>
        /// Adds a disease to the carrier's
        /// past diseases to give them immunity
        /// IF they don't already have the disease.
        /// </summary>
        public void Vaccinate(DiseaseCarrierComponent carrier, DiseasePrototype disease)
        {
            foreach (var currentDisease in carrier.Diseases)
            {
                if (currentDisease.ID == disease.ID) //ID because of the way protoypes work
                    return;
            }
            carrier.PastDiseases.Add(disease);
        }

        ///
        /// Private Events Stuff
        ///

        /// <summary>
        /// Injects the vaccine into the target
        /// if the doafter is completed
        /// </summary>
        private void OnTargetVaxxSuccessful(TargetVaxxSuccessfulEvent args)
        {
            if (args.Vaxx.Disease == null)
                return;

            Vaccinate(args.Carrier, args.Vaxx.Disease);

            _tagSystem.AddTag(args.Vaxx.Owner, "Trash");
            args.Vaxx.Used = true;

            if (TryComp<AppearanceComponent>(args.Vaxx.Owner, out var appearance))
                _appearance.SetData(args.Vaxx.Owner, ToggleVisuals.Toggled, false, appearance);
        }

        /// <summary>
        /// Cancels the vaccine doafter
        /// </summary>
        private static void OnVaxxCancelled(VaxxCancelledEvent args)
        {
            args.Vaxx.CancelToken = null;
        }
        /// These two are standard doafter stuff you can ignore
        private sealed class VaxxCancelledEvent : EntityEventArgs
        {
            public readonly DiseaseVaccineComponent Vaxx;
            public VaxxCancelledEvent(DiseaseVaccineComponent vaxx)
            {
                Vaxx = vaxx;
            }
        }
        private sealed class TargetVaxxSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public DiseaseVaccineComponent Vaxx { get; }
            public DiseaseCarrierComponent Carrier { get; }
            public TargetVaxxSuccessfulEvent(EntityUid user, EntityUid? target, DiseaseVaccineComponent vaxx, DiseaseCarrierComponent carrier)
            {
                User = user;
                Target = target;
                Vaxx = vaxx;
                Carrier = carrier;
            }
        }
    }
}
