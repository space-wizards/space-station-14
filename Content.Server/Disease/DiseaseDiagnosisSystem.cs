using System.Threading;
using Content.Server.Disease.Components;
using Content.Shared.Disease;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Examine;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Hands.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Content.Shared.Tools.Components;
using Content.Server.Station.Systems;
using Content.Shared.IdentityManagement;

namespace Content.Server.Disease
{
    /// <summary>
    /// Everything that's about disease diangosis and machines is in here
    /// </summary>
    public sealed class DiseaseDiagnosisSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;

        [Dependency] private readonly StationSystem _stationSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseSwabComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DiseaseSwabComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DiseaseDiagnoserComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, AfterInteractUsingEvent>(OnAfterInteractUsingVaccine);
            // Visuals
            SubscribeLocalEvent<DiseaseMachineComponent, PowerChangedEvent>(OnPowerChanged);
            // Private Events
            SubscribeLocalEvent<DiseaseDiagnoserComponent, DiseaseMachineFinishedEvent>(OnDiagnoserFinished);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, DiseaseMachineFinishedEvent>(OnVaccinatorFinished);
            SubscribeLocalEvent<TargetSwabSuccessfulEvent>(OnTargetSwabSuccessful);
            SubscribeLocalEvent<SwabCancelledEvent>(OnSwabCancelled);
        }

        private Queue<EntityUid> AddQueue = new();
        private Queue<EntityUid> RemoveQueue = new();

        /// <summary>
        /// This handles running disease machines
        /// to handle their delay and visuals.
        /// </summary>
        public override void Update(float frameTime)
        {
            foreach (var uid in AddQueue)
            {
                EnsureComp<DiseaseMachineRunningComponent>(uid);
            }

            AddQueue.Clear();
            foreach (var uid in RemoveQueue)
            {
                RemComp<DiseaseMachineRunningComponent>(uid);
            }

            RemoveQueue.Clear();

            foreach (var (_, diseaseMachine) in EntityQuery<DiseaseMachineRunningComponent, DiseaseMachineComponent>())
            {
                diseaseMachine.Accumulator += frameTime;

                while (diseaseMachine.Accumulator >= diseaseMachine.Delay)
                {
                    diseaseMachine.Accumulator -= diseaseMachine.Delay;
                    var ev = new DiseaseMachineFinishedEvent(diseaseMachine);
                    RaiseLocalEvent(diseaseMachine.Owner, ev);
                    RemoveQueue.Enqueue(diseaseMachine.Owner);
                }
            }
        }

        ///
        /// Event Handlers
        ///

        /// <summary>
        /// This handles using swabs on other people
        /// and checks that the swab isn't already used
        /// and the other person's mouth is accessible
        /// and then adds a random disease from that person
        /// to the swab if they have any
        /// </summary>
        private void OnAfterInteract(EntityUid uid, DiseaseSwabComponent swab, AfterInteractEvent args)
        {
            if (swab.CancelToken != null)
            {
                swab.CancelToken.Cancel();
                swab.CancelToken = null;
                return;
            }
            if (args.Target == null || !args.CanReach)
                return;

            if (!TryComp<DiseaseCarrierComponent>(args.Target, out var carrier))
                return;

            if (swab.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("swab-already-used"), args.User, Filter.Entities(args.User));
                return;
            }

            if (_inventorySystem.TryGetSlotEntity(args.Target.Value, "mask", out var maskUid) &&
                EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
                blocker.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("swab-mask-blocked", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("mask", maskUid)), args.User, Filter.Entities(args.User));
                return;
            }

            swab.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, swab.SwabDelay, swab.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetSwabSuccessfulEvent(args.User, args.Target, swab, carrier),
                BroadcastCancelledEvent = new SwabCancelledEvent(swab),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        /// <summary>
        /// This handles the disease diagnoser machine up
        /// until it's turned on. It has some slight
        /// differences in checks from the vaccinator.
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, DiseaseDiagnoserComponent component, AfterInteractUsingEvent args)
        {
            var machine = Comp<DiseaseMachineComponent>(uid);
            if (args.Handled || !args.CanReach)
                return;

            if (HasComp<DiseaseMachineRunningComponent>(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (!HasComp<HandsComponent>(args.User) || HasComp<ToolComponent>(args.Used)) // Don't want to accidentally breach wrenching or whatever
                return;

            if (!TryComp<DiseaseSwabComponent>(args.Used, out var swab))
            {
                _popupSystem.PopupEntity(Loc.GetString("diagnoser-cant-use-swab", ("machine", uid), ("swab", args.Used)), uid, Filter.Entities(args.User));
                return;
            }
            _popupSystem.PopupEntity(Loc.GetString("machine-insert-item", ("machine", uid), ("item", args.Used), ("user", args.User)), uid, Filter.Entities(args.User));


            machine.Disease = swab.Disease;
            EntityManager.DeleteEntity(args.Used);

            AddQueue.Enqueue(uid);
            UpdateAppearance(uid, true, true);
            SoundSystem.Play("/Audio/Machines/diagnoser_printing.ogg", Filter.Pvs(uid), uid);
        }

        /// <summary>
        /// This handles the vaccinator machine up
        /// until it's turned on. It has some slight
        /// differences in checks from the diagnoser.
        /// </summary>
        private void OnAfterInteractUsingVaccine(EntityUid uid, DiseaseVaccineCreatorComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (HasComp<DiseaseMachineRunningComponent>(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (!HasComp<HandsComponent>(args.User) || HasComp<ToolComponent>(args.Used)) //This check ensures tools don't break without yaml ordering jank
                return;

            if (!TryComp<DiseaseSwabComponent>(args.Used, out var swab) || swab.Disease == null || !swab.Disease.Infectious)
            {
                _popupSystem.PopupEntity(Loc.GetString("diagnoser-cant-use-swab", ("machine", uid), ("swab", args.Used)), uid, Filter.Entities(args.User));
                return;
            }
            _popupSystem.PopupEntity(Loc.GetString("machine-insert-item", ("machine", uid), ("item", args.Used), ("user", args.User)), uid, Filter.Entities(args.User));
            var machine = Comp<DiseaseMachineComponent>(uid);
            machine.Disease = swab.Disease;
            EntityManager.DeleteEntity(args.Used);

            AddQueue.Enqueue(uid);
            UpdateAppearance(uid, true, true);
            SoundSystem.Play("/Audio/Machines/vaccinator_running.ogg", Filter.Pvs(uid), uid);
        }

        /// <summary>
        /// This handles swab examination text
        /// so you can tell if they are used or not.
        /// </summary>
        private void OnExamined(EntityUid uid, DiseaseSwabComponent swab, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (swab.Used)
                    args.PushMarkup(Loc.GetString("swab-used"));
                else
                    args.PushMarkup(Loc.GetString("swab-unused"));
            }
        }

        ///
        /// Helper functions
        ///

        /// <summary>
        /// This assembles a disease report
        /// With its basic details and
        /// specific cures (i.e. not spaceacillin).
        /// The cure resist field tells you how
        /// effective spaceacillin etc will be.
        /// </summary>
        private FormattedMessage AssembleDiseaseReport(DiseasePrototype disease)
        {
            FormattedMessage report = new();
            report.AddMarkup(Loc.GetString("diagnoser-disease-report-name", ("disease", disease.Name)));
            report.PushNewline();

            if (disease.Infectious)
            {
                report.AddMarkup(Loc.GetString("diagnoser-disease-report-infectious"));
                report.PushNewline();
            } else
            {
                report.AddMarkup(Loc.GetString("diagnoser-disease-report-not-infectious"));
                report.PushNewline();
            }
            string cureResistLine = string.Empty;
            cureResistLine += disease.CureResist switch
            {
                < 0f => Loc.GetString("diagnoser-disease-report-cureresist-none"),
                <= 0.05f => Loc.GetString("diagnoser-disease-report-cureresist-low"),
                <= 0.14f => Loc.GetString("diagnoser-disease-report-cureresist-medium"),
                _ => Loc.GetString("diagnoser-disease-report-cureresist-high")
            };
            report.AddMarkup(cureResistLine);
            report.PushNewline();

            // Add Cures
            if (disease.Cures.Count == 0)
            {
                report.AddMarkup(Loc.GetString("diagnoser-no-cures"));
            }
            else
            {
                report.PushNewline();
                report.AddMarkup(Loc.GetString("diagnoser-cure-has"));
                report.PushNewline();

                foreach (var cure in disease.Cures)
                {
                    report.AddMarkup(cure.CureText());
                    report.PushNewline();
                }
            }

            return report;
        }

        public bool ServerHasDisease(DiseaseServerComponent server, DiseasePrototype disease)
        {
            bool has = false;
            foreach (var serverDisease in server.Diseases)
            {
                if (serverDisease.ID == disease.ID)
                    has = true;
            }
            return has;
        }
        ///
        /// Appearance stuff
        ///

        /// <summary>
        /// Appearance helper function to
        /// set the component's power and running states.
        /// </summary>
        private void UpdateAppearance(EntityUid uid, bool isOn, bool isRunning)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(DiseaseMachineVisuals.IsOn, isOn);
            appearance.SetData(DiseaseMachineVisuals.IsRunning, isRunning);
        }
        /// <summary>
        /// Makes sure the machine is visually off/on.
        /// </summary>
        private void OnPowerChanged(EntityUid uid, DiseaseMachineComponent component, ref PowerChangedEvent args)
        {
            UpdateAppearance(uid, args.Powered, false);
        }
        ///
        /// Private events
        ///

        /// <summary>
        /// Copies a disease prototype to the swab
        /// after the doafter completes.
        /// </summary>
        private void OnTargetSwabSuccessful(TargetSwabSuccessfulEvent args)
        {
            if (args.Target == null)
                return;

            args.Swab.Used = true;
            _popupSystem.PopupEntity(Loc.GetString("swab-swabbed", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.Target.Value, Filter.Entities(args.User));

            if (args.Swab.Disease != null || args.Carrier.Diseases.Count == 0)
                return;

            args.Swab.Disease = _random.Pick(args.Carrier.Diseases);
        }

        /// <summary>
        /// Cancels the swab doafter if needed.
        /// </summary>
        private static void OnSwabCancelled(SwabCancelledEvent args)
        {
            args.Swab.CancelToken = null;
        }

        /// <summary>
        /// Prints a diagnostic report with its findings.
        /// Also cancels the animation.
        /// </summary>
        private void OnDiagnoserFinished(EntityUid uid, DiseaseDiagnoserComponent component, DiseaseMachineFinishedEvent args)
        {
            var isPowered = this.IsPowered(uid, EntityManager);
            UpdateAppearance(uid, isPowered, false);
            // spawn a piece of paper.
            var printed = Spawn(args.Machine.MachineOutput, Transform(uid).Coordinates);

            if (!TryComp<PaperComponent>(printed, out var paper))
                return;

            string reportTitle;
            FormattedMessage contents = new();
            if (args.Machine.Disease != null)
            {
                reportTitle = Loc.GetString("diagnoser-disease-report", ("disease", args.Machine.Disease.Name));
                contents = AssembleDiseaseReport(args.Machine.Disease);

                var known = false;

                foreach (var server in EntityQuery<DiseaseServerComponent>(true))
                {
                    if (_stationSystem.GetOwningStation(server.Owner) != _stationSystem.GetOwningStation(uid))
                        continue;

                    if (ServerHasDisease(server, args.Machine.Disease))
                    {
                       known = true;
                    }
                    else
                    {
                        server.Diseases.Add(args.Machine.Disease);
                    }
                }

                if (!known)
                {
                    Spawn("ResearchDisk5000", Transform(uid).Coordinates);
                }
            }
            else
            {
                reportTitle = Loc.GetString("diagnoser-disease-report-none");
                contents.AddMarkup(Loc.GetString("diagnoser-disease-report-none-contents"));
            }
            MetaData(printed).EntityName = reportTitle;

            _paperSystem.SetContent(printed, contents.ToMarkup(), paper);
        }

        /// <summary>
        /// Prints a vaccine that will vaccinate
        /// against the disease on the inserted swab.
        /// </summary>
        private void OnVaccinatorFinished(EntityUid uid, DiseaseVaccineCreatorComponent component, DiseaseMachineFinishedEvent args)
        {
            UpdateAppearance(uid, this.IsPowered(uid, EntityManager), false);

            // spawn a vaccine
            var vaxx = Spawn(args.Machine.MachineOutput, Transform(uid).Coordinates);

            if (!TryComp<DiseaseVaccineComponent>(vaxx, out var vaxxComp))
                return;

            vaxxComp.Disease = args.Machine.Disease;
        }

        /// <summary>
        /// Cancels the mouth-swabbing doafter
        /// </summary>
        private sealed class SwabCancelledEvent : EntityEventArgs
        {
            public readonly DiseaseSwabComponent Swab;
            public SwabCancelledEvent(DiseaseSwabComponent swab)
            {
                Swab = swab;
            }
        }

        /// <summary>
        /// Fires if the doafter for swabbing someone's mouth succeeds
        /// </summary>
        private sealed class TargetSwabSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public DiseaseSwabComponent Swab { get; }

            public DiseaseCarrierComponent Carrier { get; }

            public TargetSwabSuccessfulEvent(EntityUid user, EntityUid? target, DiseaseSwabComponent swab, DiseaseCarrierComponent carrier)
            {
                User = user;
                Target = target;
                Swab = swab;
                Carrier = carrier;
            }
        }

        /// <summary>
        /// Fires when a disease machine is done
        /// with its production delay and ready to
        /// create a report or vaccine
        /// </summary>
        private sealed class DiseaseMachineFinishedEvent : EntityEventArgs
        {
            public DiseaseMachineComponent Machine {get;}
            public DiseaseMachineFinishedEvent(DiseaseMachineComponent machine)
            {
                Machine = machine;
            }
        }
    }
}

