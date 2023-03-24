using Content.Server.Players;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Shared.Roles;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
namespace Content.Server.Flash
{
    internal sealed class RevoHeadFlashSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [Dependency] private readonly DamageableSystem _damageSystem = default!;

        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

        [Dependency] private readonly IChatManager _chatManager = default!;

        private const string RevolutionaryPrototypeId = "Revolutionary";
        private const string RevolutionaryHeadPrototypeId = "RevolutionaryHead";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlashEvent>(RevoFlash);
            SubscribeLocalEvent<MobStateChangedEvent>(OnDead);
        }

        public void RevoFlash(FlashEvent ev)
        {
            if (!TryComp<MindComponent>(ev.User, out var usermindcomp) || usermindcomp.Mind is null )
                return;
            foreach (var role in usermindcomp.Mind.AllRoles)
            {
                if (role is not TraitorRole traitor)
                    continue;
                if (traitor.Prototype.ID == RevolutionaryHeadPrototypeId)
                {
                    Convert(ev.Target, ev);
                }
            }
        }

        private void Convert(EntityUid target, FlashEvent ev)
        {
            if (!TryComp<MindComponent>(target, out var targetmindcomp) || targetmindcomp.Mind is null || targetmindcomp.Mind.CurrentJob is null)
                return;

            if (!_mobStateSystem.IsDead(target))
                return;

            // Command or Sec cant become Antag on default
            if(!(targetmindcomp.Mind.CurrentJob.Prototype.CanBeAntag))
                return;

            if (targetmindcomp.Mind.HasRole<TraitorRole>())
                return;

            var antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryPrototypeId);
            var revoRole = new TraitorRole(targetmindcomp.Mind, antagPrototype);
            targetmindcomp.Mind.AddRole(revoRole);
            // Revive and heal the target with low health shitcode
            RejuvenateCommand.PerformRejuvenate(target);
            if (HasComp<DamageableComponent>(target))
            {
                var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 70);
                _damageSystem.TryChangeDamage(target, damage, true);
            }
            SoundSystem.Play("/Audio/Magic/staff_chaos.ogg", Filter.Empty().AddWhere(s => ((IPlayerSession)s).Data.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false), AudioParams.Default);
            GreetConvert(targetmindcomp);
        }
        private void GreetConvert(MindComponent mindComponent)
        {
            var message = Loc.GetString("revolution-role-greeting");
            var messageWrapper = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

            if (mindComponent.Mind?.OwnedEntity is null)
                return;

            if (mindComponent.Mind.Session == null)
                return;

            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
               messageWrapper, default, false, mindComponent.Mind.Session.ConnectedClient, Color.Red);
        }

        public void OnDead(MobStateChangedEvent ev)
        {
            if (!TryComp<MindComponent>(ev.Target, out var targetmindcomp) || targetmindcomp.Mind is null || targetmindcomp.Mind.CurrentJob is null)
                return;

            if (!targetmindcomp.Mind.HasRole<TraitorRole>())
                return;

            if (!_mobStateSystem.IsDead(ev.Target))
                return;

            foreach (var role in targetmindcomp.Mind.AllRoles)
            {
                if (role is not TraitorRole traitor)
                    continue;

                if (traitor.Prototype.ID == RevolutionaryPrototypeId)
                {
                    targetmindcomp.Mind.RemoveRole(role);
                    // Revive and heal the target with low health shitcode
                    RejuvenateCommand.PerformRejuvenate(ev.Target);
                    if (HasComp<DamageableComponent>(ev.Target))
                    {
                        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 70);
                        _damageSystem.TryChangeDamage(ev.Target, damage, true);
                    }
                    GreetDeConvert(targetmindcomp);
                }
            }
        }
        private void GreetDeConvert(MindComponent mindComponent)
        {
            var message = Loc.GetString("revolution-deconvert-greeting");
            var messageWrapper = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            if (mindComponent.Mind?.OwnedEntity is null)
                return;
            if (mindComponent.Mind.Session == null)
                return;

            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
               messageWrapper, default, false, mindComponent.Mind.Session.ConnectedClient, Color.Green);
        }
    }
}
