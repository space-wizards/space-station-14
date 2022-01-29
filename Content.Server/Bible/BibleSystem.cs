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
        [Dependency] private readonly InventorySystem _invSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;



        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BibleComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, BibleComponent component, AfterInteractEvent args)
        {
            var currentTime = _gameTiming.CurTime;

            if (currentTime < component.CooldownEnd)
            {
                return;
            }
            if (args.Target == null || args.Target == args.User || !EntityManager.HasComponent<MobStateComponent>(args.Target))
            {
                return;
            }

            component.LastAttackTime = currentTime;
            component.CooldownEnd = component.LastAttackTime + TimeSpan.FromSeconds(component.CooldownTime);
            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(component.LastAttackTime, component.CooldownEnd), false);

            if (!EntityManager.HasComponent<BibleUserComponent>(args.User))
            {
                args.User.PopupMessage(Loc.GetString("bible-sizzle"));

                SoundSystem.Play(Filter.Pvs(args.User), "/Audio/Effects/lightburn.ogg");
                _damageableSystem.TryChangeDamage(args.User, component.DamageOnUntrainedUse, true);

                return;
            }

            if (!_invSystem.TryGetSlotEntity(args.Target.Value, "head", out var entityUid))
            {
                if (_random.Prob(component.FailChance))
                {
                var othersFailMessage = Loc.GetString("bible-heal-fail-others", ("user", args.User),("target", args.Target),("bible", uid));
                args.User.PopupMessageOtherClients(othersFailMessage);

                var selfFailMessage = Loc.GetString("bible-heal-fail-self", ("target", args.Target),("bible", uid));
                args.User.PopupMessage(selfFailMessage);

                SoundSystem.Play(Filter.Pvs(args.Target.Value), "/Audio/Effects/hit_kick.ogg");
                _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageOnFail, true);
                return;
                }
            }

            var othersMessage = Loc.GetString("bible-heal-success-others", ("user", args.User),("target", args.Target),("bible", uid));
            args.User.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("bible-heal-success-self", ("target", args.Target),("bible", uid));
            args.User.PopupMessage(selfMessage);

            SoundSystem.Play(Filter.Pvs(args.Target.Value), "/Audio/Effects/holy.ogg");
            _damageableSystem.TryChangeDamage(args.Target.Value, component.Damage, true);
        }

    }
}
