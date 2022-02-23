using Content.Server.Act;
using Content.Server.Actions.Events;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Weapon.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly MeleeWeaponSystem _meleeWeaponSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedCombatModeComponent, DisarmActionEvent>(OnEntityActionPerform);
        }

        private void OnEntityActionPerform(EntityUid uid, SharedCombatModeComponent component, DisarmActionEvent args)
        {
            if (args.Handled)
                return;

            if (!_actionBlockerSystem.CanAttack(args.Performer))
                return;

            var attemptEvent = new DisarmAttemptEvent(args.Target, args.Performer);
            RaiseLocalEvent(args.Target, attemptEvent);
            if (attemptEvent.Cancelled)
                return;

            var diff = Transform(args.Target).MapPosition.Position - Transform(args.Performer).MapPosition.Position;
            var angle = Angle.FromWorldVec(diff);

            var filterAll = Filter.Pvs(args.Performer);
            var filterOther = filterAll.RemoveWhereAttachedEntity(e => e == args.Performer);

            args.Handled = true;

            if (_random.Prob(component.DisarmFailChance))
            {
                SoundSystem.Play(Filter.Pvs(args.Performer), component.DisarmFailSound.GetSound(), args.Performer, AudioHelpers.WithVariation(0.025f));

                var targetName = Name(args.Target);

                var msgOther = Loc.GetString(
                    "disarm-action-popup-message-other-clients",
                    ("performerName", Name(args.Performer)),
                    ("targetName", targetName));

                var msgUser = Loc.GetString("disarm-action-popup-message-cursor", ("targetName", targetName ));

                _popupSystem.PopupEntity(msgOther, args.Performer, filterOther);
                _popupSystem.PopupEntity(msgUser, args.Performer, Filter.Entities(args.Performer));

                _meleeWeaponSystem.SendLunge(angle, args.Performer);
                return;
            }

            _meleeWeaponSystem.SendAnimation("disarm", angle, args.Performer, args.Performer, new[] { args.Target });
            SoundSystem.Play(filterAll, component.DisarmSuccessSound.GetSound(), args.Performer, AudioHelpers.WithVariation(0.025f));
            _logSystem.Add(LogType.DisarmedAction, $"{ToPrettyString(args.Performer):user} used disarm on {ToPrettyString(args.Target):target}");

            var eventArgs = new DisarmedEvent() { Target = args.Target, Source = args.Performer, PushProbability = component.DisarmPushChance };
            RaiseLocalEvent(args.Target, eventArgs);
        }
    }
}
