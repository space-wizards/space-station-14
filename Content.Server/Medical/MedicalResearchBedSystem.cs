using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Buckle.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Station.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using static Content.Shared.MedicalScanner.SharedMedicalResearchBedComponent;
using Robust.Shared.Timing;


namespace Content.Server.Medical
{
    public sealed class MedicalResearchBedSystem : EntitySystem
    {

        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private const float UpdateRate = 3f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MedicalResearchBedComponent, ActivateInWorldEvent>(HandleActivateEvent);
            SubscribeLocalEvent<MedicalResearchBedComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
            SubscribeLocalEvent<MedicalResearchBedComponent, BuckleChangeEvent>(OnBuckleChange);
        }

        private void OnBuckleChange(EntityUid uid, MedicalResearchBedComponent medicalResearchBed, BuckleChangeEvent args)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            if (entities.TryGetComponent<MedicalResearchBedServerComponent>(uid, out var server))
                server.bedChange = true;

        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // check update rate
            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;
            _updateDif = 0f;

            var medicalResearchBeds = EntityManager.EntityQuery<MedicalResearchBedComponent>();
            foreach (var medicalResearchBed in medicalResearchBeds)
            {
                UpdateInterface(medicalResearchBed.Owner, medicalResearchBed);
            }
        }

        private void HandleActivateEvent(EntityUid uid, MedicalResearchBedComponent medicalResearchBed, ActivateInWorldEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            OpenUserInterface(args.User, medicalResearchBed);

            UpdateInterface(uid,medicalResearchBed);
        }

        private void HandleActivateVerb(EntityUid uid, MedicalResearchBedComponent medicalResearchBed, GetVerbsEvent<ActivationVerb> args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            OpenUserInterface(args.User, medicalResearchBed);

            UpdateInterface(uid,medicalResearchBed);
        }

        private void UpdateInterface(EntityUid uid, MedicalResearchBedComponent medicalResearchBed)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            var strap = Comp<StrapComponent>(uid);
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                if (TryComp<SolutionContainerManagerComponent>(buckledEntity, out var solutions))
                {
                    if (entities.TryGetComponent<DamageableComponent>(buckledEntity, out var damageable))
                    {
                        if (entities.TryGetComponent<MedicalResearchBedServerComponent>(uid, out var server))
                        {

                            if (server.bedChange)
                            {
                                server.bedChange = false;
                                server.lastHealthRecording = damageable.TotalDamage;
                            }
                            else
                            {
                                if (damageable.TotalDamage < server.lastHealthRecording)
                                {
                                    server.healthChanges += server.lastHealthRecording - damageable.TotalDamage;
                                    server.lastHealthRecording = damageable.TotalDamage;
                                }
                                else
                                {
                                    server.lastHealthRecording = damageable.TotalDamage;
                                }
                            }

                            //Console.WriteLine(server.healthChanges);
                            if (server.healthChanges >= medicalResearchBed.HealthGoal && !server.diskPrinted)
                            {
                                Spawn(medicalResearchBed.ResearchDiskReward, Transform(uid).Coordinates);
                                server.diskPrinted = true;
                            }

                            medicalResearchBed.UserInterface?.SendMessage(new MedicalResearchBedScannedUserMessage(buckledEntity, solutions.Solutions["chemicals"].Contents,server.healthChanges));
                        }
                    }
                }
            }
        }

        private void OpenUserInterface(EntityUid user, MedicalResearchBedComponent medicalResearchBed)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            medicalResearchBed.UserInterface?.Open(actor.PlayerSession);
        }

        private void AddOpenUiVerb(EntityUid uid, MedicalResearchBedComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            ActivationVerb verb = new();
            verb.Act = () => HandleActivateVerb(uid, component, args);
            verb.Text = "Open UI";
            args.Verbs.Add(verb);
        }

    }
}
