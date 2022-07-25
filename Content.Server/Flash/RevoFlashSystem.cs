using Content.Server.Players;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlashEvent>(RevoFlash);
        }

        public void RevoFlash(FlashEvent ev)
        {
            if (!TryComp<MindComponent>(ev.Target, out MindComponent? usermindcomp) || usermindcomp.Mind is null) 
                return;
            
            foreach (var role in usermindcomp.Mind.AllRoles)
            {
                // If the user has the revo head role they can use this flash to convert ppl
                if (role.Name == "Revolutionary Head")
                {
                    Convert(ev);
                    return;
                }
                else if (role.Name != "Revolutionary")
                {
                    Revert(ev);
                    return;
                }
            }
        }

        private void Convert(FlashEvent ev)
        {
            if (!TryComp<MindComponent>(ev.Target, out MindComponent? targetmindcomp) || targetmindcomp.Mind is null || targetmindcomp.Mind.CurrentJob is null) 
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

        private void Revert(FlashEvent ev)
        {
            if (!TryComp<MindComponent>(ev.User, out MindComponent? mind) || mind.Mind is null)
                return;
            var rnd = new Random();
            Logger.Error(rnd+" "+ev.User);
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role.Name == "Revolutionary" && rnd.Next(10) > 2)
                    mind.Mind.RemoveRole(role);

                SoundSystem.Play("/Audio/Magic/staff_change.ogg", Filter.Empty().AddWhere(s => ((IPlayerSession)s).Data.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false), AudioParams.Default);
            }
        }
        
    }
}
