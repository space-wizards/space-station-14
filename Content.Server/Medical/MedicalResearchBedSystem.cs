using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Buckle.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using static Content.Shared.MedicalScanner.SharedMedicalResearchBedComponent;

namespace Content.Server.Medical
{
    public sealed class MedicalResearchBedSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MedicalResearchBedComponent, ActivateInWorldEvent>(HandleActivateEvent);
            SubscribeLocalEvent<MedicalResearchBedComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
        }

        private void HandleActivateEvent(EntityUid uid, MedicalResearchBedComponent medicalResearchBed, ActivateInWorldEvent args)
        {
            OpenUserInterface(args.User, medicalResearchBed);

            //TODO check power

            var strap = Comp<StrapComponent>(args.Target);
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                if (TryComp<SolutionContainerManagerComponent>(buckledEntity, out var solutions))
                    medicalResearchBed.UserInterface?.SendMessage(new MedicalResearchBedScannedUserMessage(buckledEntity, solutions.Solutions["chemicals"].Contents));
            }         
        }

        private void HandleActivateVerb(EntityUid uid, MedicalResearchBedComponent medicalResearchBed, GetVerbsEvent<ActivationVerb> args)
        {
            OpenUserInterface(args.User, medicalResearchBed);

            //TODO check power

            var strap = Comp<StrapComponent>(args.Target);
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                //Console.WriteLine(buckledEntity);
                if (TryComp<SolutionContainerManagerComponent>(buckledEntity, out var solutions))
                {

                    medicalResearchBed.UserInterface?.SendMessage(new MedicalResearchBedScannedUserMessage(buckledEntity, solutions.Solutions["chemicals"].Contents));
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

            ActivationVerb verb = new();
            verb.Act = () => HandleActivateVerb(uid, component, args);
            verb.Text = "Toggle UI";
            args.Verbs.Add(verb);
        }

    }
}
