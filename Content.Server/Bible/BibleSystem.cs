using System;
using Robust.Shared.GameObjects;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.MobState.Components;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Server.Cooldown;
using Content.Server.Inventory;
using Content.Server.Mind.Components;
using Content.Server.Bible.Components;
using Content.Server.Popups;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Localization;
using Robust.Shared.Timing;


namespace Content.Server.Bible
{
    public class BibleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BibleComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, BibleComponent component, AfterInteractEvent args)
        {
            var invSystem = EntitySystem.Get<InventorySystem>();
            var random = IoCManager.Resolve<IRobustRandom>();
            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime;
            EntityManager.TryGetComponent<MetaDataComponent>(uid, out var meta);

            if (currentTime < component.CooldownEnd)
            {
                return;
            }
            if (args.Target == null)
            {
                return;
            }
            if (!EntityManager.HasComponent<MobStateComponent>(args.Target))
            {
                return;
            }
            if (!EntityManager.HasComponent<BibleUserComponent>(args.User))
            {
                args.User.PopupMessage(Loc.GetString("bible-sizzle"));

                SoundSystem.Play(Filter.Pvs(args.User), "/Audio/Effects/lightburn.ogg");
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(args.User, component.DamageOnUntrainedUse, true);

                return;
            }
            if (args.Target == args.User)
            {
                return;
            }

            component.LastAttackTime = currentTime;
            component.CooldownEnd = component.LastAttackTime + TimeSpan.FromSeconds(component.CooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(component.LastAttackTime, component.CooldownEnd), false);

            if (!invSystem.TryGetSlotEntity(args.Target.Value, "head", out var entityUid))
            {
                if (random.Prob(0.34f))
                {
                var othersFailMessage = Loc.GetString("bible-heal-fail-others", ("user", args.User),("target", args.Target),("bible",meta.EntityName));
                args.User.PopupMessageOtherClients(othersFailMessage);

                var selfFailMessage = Loc.GetString("bible-heal-fail-self", ("target", args.Target),("bible",meta.EntityName));
                args.User.PopupMessage(selfFailMessage);

                SoundSystem.Play(Filter.Pvs(args.Target.Value), "/Audio/Effects/hit_kick.ogg");
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(args.Target.Value, component.DamageOnFail, true);
                return;
                }
            }

            var othersMessage = Loc.GetString("bible-heal-success-others", ("user", args.User),("target", args.Target),("bible",meta.EntityName));
            args.User.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("bible-heal-success-self", ("target", args.Target),("bible",meta.EntityName));
            args.User.PopupMessage(selfMessage);

            SoundSystem.Play(Filter.Pvs(args.Target.Value), "/Audio/Effects/holy.ogg");
            EntitySystem.Get<DamageableSystem>().TryChangeDamage(args.Target.Value, component.Damage, true);
        }

    }
}
