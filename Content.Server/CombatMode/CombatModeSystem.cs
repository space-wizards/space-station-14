using Content.Server.Actions.Events;
using Content.Server.Administration.Logs;
using Content.Server.CombatMode.Disarm;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Weapon.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Physics;

namespace Content.Server.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly MeleeWeaponSystem _meleeWeaponSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
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

            if (TryComp<HandsComponent>(args.Performer, out var hands)
                && hands.ActiveHand != null
                && !hands.ActiveHand.IsEmpty)
            {
                _popupSystem.PopupEntity(Loc.GetString("disarm-action-free-hand"), args.Performer, Filter.Entities(args.Performer));
                return;
            }

            EntityUid? inTargetHand = null;

            if (TryComp<HandsComponent>(args.Target, out HandsComponent? targetHandsComponent)
                && targetHandsComponent.ActiveHand != null
                && !targetHandsComponent.ActiveHand.IsEmpty)
            {
                inTargetHand = targetHandsComponent.ActiveHand.HeldEntity!.Value;
            }

            var attemptEvent = new DisarmAttemptEvent(args.Target, args.Performer,inTargetHand);

            if (inTargetHand != null)
            {
                RaiseLocalEvent(inTargetHand.Value, attemptEvent, true);
            }
            RaiseLocalEvent(args.Target, attemptEvent, true);
            if (attemptEvent.Cancelled)
                return;

            var diff = Transform(args.Target).MapPosition.Position - Transform(args.Performer).MapPosition.Position;
            var angle = Angle.FromWorldVec(diff);

            var filterAll = Filter.Pvs(args.Performer);
            var filterOther = filterAll.RemoveWhereAttachedEntity(e => e == args.Performer);

            args.Handled = true;
            var chance = CalculateDisarmChance(args.Performer, args.Target, inTargetHand, component);
            if (_random.Prob(chance))
            {
                SoundSystem.Play(component.DisarmFailSound.GetSound(), Filter.Pvs(args.Performer), args.Performer, AudioHelpers.WithVariation(0.025f));

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
            SoundSystem.Play(component.DisarmSuccessSound.GetSound(), filterAll, args.Performer, AudioHelpers.WithVariation(0.025f));
            _adminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(args.Performer):user} used disarm on {ToPrettyString(args.Target):target}");

            var eventArgs = new DisarmedEvent() { Target = args.Target, Source = args.Performer, PushProbability = chance };
            RaiseLocalEvent(args.Target, eventArgs, true);
        }


        private float CalculateDisarmChance(EntityUid disarmer, EntityUid disarmed, EntityUid? inTargetHand, SharedCombatModeComponent disarmerComp)
        {
            float healthMod = 0;
            if (TryComp<DamageableComponent>(disarmer, out var disarmerDamage) && TryComp<DamageableComponent>(disarmed, out var disarmedDamage))
            {
                // I wanted this to consider their mob state thresholds too but I'm not touching that shitcode after having a go at this.
                healthMod = (((float) disarmedDamage.TotalDamage - (float) disarmerDamage.TotalDamage) / 200); // Ex. You have 0 damage, they have 90, you get a 45% chance increase
            }

            float massMod = 0;

            if (TryComp<PhysicsComponent>(disarmer, out var disarmerPhysics) && TryComp<PhysicsComponent>(disarmed, out var disarmedPhysics))
            {
                if (disarmerPhysics.FixturesMass != 0) // yeah this will never happen but let's not kill the server if it does
                    massMod = (((disarmedPhysics.FixturesMass / disarmerPhysics.FixturesMass - 1 ) / 2)); // Ex, you weigh 120, they weigh 70, you get a 29% bonus
            }

            float chance = (disarmerComp.BaseDisarmFailChance - healthMod - massMod);
            if (HasComp<SlowedDownComponent>(disarmer)) // might need to revisit this part after stamina damage, right now this is basically "pre-stun"
                chance += 0.35f;
            if (HasComp<SlowedDownComponent>(disarmed))
                chance -= 0.35f;

            if (inTargetHand != null && TryComp<DisarmMalusComponent>(inTargetHand, out var malus))
            {
                chance += malus.Malus;
            }

            return Math.Clamp(chance, 0f, 1f);
        }
    }
}
