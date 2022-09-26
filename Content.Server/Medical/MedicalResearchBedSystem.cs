using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Buckle.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
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
        }

        private void HandleActivateEvent(EntityUid uid, MedicalResearchBedComponent medicalResearchBed, ActivateInWorldEvent args)
        {
            OpenUserInterface(args.User, medicalResearchBed);

            var strap = Comp<StrapComponent>(args.Target);
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                Console.WriteLine(buckledEntity);
                medicalResearchBed.UserInterface?.SendMessage(new MedicalResearchBedScannedUserMessage(buckledEntity));
            }
            
            
        }

        private void OpenUserInterface(EntityUid user, MedicalResearchBedComponent medicalResearchBed)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            medicalResearchBed.UserInterface?.Open(actor.PlayerSession);
        }
       
    }
}
