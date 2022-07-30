using Content.Server.Players;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Server.Flash.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
namespace Content.Server.Flash
{
    internal sealed class RevoHeadFlashSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const string RevolutionaryPrototypeId = "Revolutionary";
        private const string RevolutionaryHeadPrototypeId = "RevolutionaryHead";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlashableComponent,FlashEvent>(RevoFlash);
        }

        public void RevoFlash(EntityUid target, FlashableComponent comp, FlashEvent ev)
        {
            if (!TryComp<MindComponent>(ev.User, out var usermindcomp) || usermindcomp.Mind is null) 
                return;
            
            foreach (var role in usermindcomp.Mind.AllRoles)
            {
                if (role is not TraitorRole traitor) 
                    continue;
                if (traitor.Prototype.ID == RevolutionaryHeadPrototypeId)
                {
                    Convert(target, ev);
                }
            }
        }

        private void Convert(EntityUid target, FlashEvent ev)
        {
            if (!TryComp<MindComponent>(target, out var targetmindcomp) || targetmindcomp.Mind is null || targetmindcomp.Mind.CurrentJob is null) 
                return;
            
            foreach (var department in targetmindcomp.Mind.CurrentJob.Prototype.Departments)
            {
                if (targetmindcomp.Mind.HasRole<TraitorRole>()) 
                    return;
                if (department != "Command" || department != "Security")
                {
                    var antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryPrototypeId);
                    var revoRole = new TraitorRole(targetmindcomp.Mind, antagPrototype);
                    targetmindcomp.Mind.AddRole(revoRole);

                    SoundSystem.Play("/Audio/Magic/staff_chaos.ogg", Filter.Empty().AddWhere(s => ((IPlayerSession)s).Data.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false), AudioParams.Default);
                }
            }            
        }        
    }
}
