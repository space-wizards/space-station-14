using Content.Server.Bible.Components;
using Content.Server.Body.Systems;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Bible;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Bible
{
    public sealed class BibleSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly InventorySystem _invSystem = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedRottingSystem _rotting = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly UseDelaySystem _delay = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BibleComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<SummonableComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
            SubscribeLocalEvent<SummonableComponent, GetItemActionsEvent>(GetSummonAction);
            SubscribeLocalEvent<SummonableComponent, SummonActionEvent>(OnSummon);
            SubscribeLocalEvent<FamiliarComponent, MobStateChangedEvent>(OnFamiliarDeath);
            SubscribeLocalEvent<FamiliarComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        }

        private readonly Queue<EntityUid> _addQueue = new();
        private readonly Queue<EntityUid> _remQueue = new();

        /// <summary>
        /// This handles familiar respawning.
        /// </summary>
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in _addQueue)
            {
                EnsureComp<SummonableRespawningComponent>(entity);
            }
            _addQueue.Clear();

            foreach (var entity in _remQueue)
            {
                RemComp<SummonableRespawningComponent>(entity);
            }
            _remQueue.Clear();

            var query = EntityQueryEnumerator<SummonableRespawningComponent, SummonableComponent>();
            while (query.MoveNext(out var uid, out var _, out var summonableComp))
            {
                summonableComp.Accumulator += frameTime;
                if (summonableComp.Accumulator < summonableComp.RespawnTime)
                {
                    continue;
                }
                // Clean up the old body
                if (summonableComp.Summon != null)
                {
                    Del(summonableComp.Summon.Value);
                    summonableComp.Summon = null;
                }
                summonableComp.AlreadySummoned = false;
                _popupSystem.PopupEntity(Loc.GetString("bible-summon-respawn-ready", ("book", uid)), uid, PopupType.Medium);
                _audio.PlayPvs(summonableComp.SummonSound, uid);
                // Clean up the accumulator and respawn tracking component
                summonableComp.Accumulator = 0;
                _remQueue.Enqueue(uid);
            }
        }

        private void OnAfterInteract(EntityUid uid, BibleComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp(uid, out UseDelayComponent? useDelay) || _delay.IsDelayed((uid, useDelay)))
                return;

            if (args.Target == null || args.Target == args.User)
                return;

            if (_mobStateSystem.IsDead(args.Target.Value))
            {
                TryReviveDeadTarget((uid, component), args.User, args.Target.Value, useDelay);
                return;
            }

            if (!_mobStateSystem.IsAlive(args.Target.Value))
                return;

            if (!HasComp<BibleUserComponent>(args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-sizzle"), args.User, args.User);

                _audio.PlayPvs(component.SizzleSoundPath, args.User);
                _damageableSystem.TryChangeDamage(args.User, component.DamageOnUntrainedUse, true, origin: uid);
                _delay.TryResetDelay((uid, useDelay));

                return;
            }

            var userEnt = Identity.Entity(args.User, EntityManager);
            var targetEnt = Identity.Entity(args.Target.Value, EntityManager);

            // This only has a chance to fail if the target is not wearing anything on their head and is not a familiar.
            if (!_invSystem.TryGetSlotEntity(args.Target.Value, "head", out _) && !HasComp<FamiliarComponent>(args.Target.Value))
            {
                if (_random.Prob(component.FailChance))
                {
                    var othersFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
                    _popupSystem.PopupEntity(othersFailMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);

                    var selfFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-self", ("target", targetEnt), ("bible", uid));
                    _popupSystem.PopupEntity(selfFailMessage, args.User, args.User, PopupType.MediumCaution);

                    _audio.PlayPvs(component.BibleHitSound, args.User);
                    _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageOnFail, true, origin: uid);
                    _delay.TryResetDelay((uid, useDelay));
                    return;
                }
            }

            string othersMessage;
            string selfMessage;

            if (_damageableSystem.TryChangeDamage(args.Target.Value, component.Damage, true, origin: uid))
            {
                othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
                selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-self", ("target", targetEnt), ("bible", uid));

                _audio.PlayPvs(component.HealSoundPath, args.User);
                _delay.TryResetDelay((uid, useDelay));

                if (component.HealingLightEffect.HasValue)
                    Spawn(component.HealingLightEffect.Value, new EntityCoordinates(args.Target.Value, default));
            }
            else
            {
                othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
                selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-self", ("target", targetEnt), ("bible", uid));
            }

            _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);
            _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
        }

        private void TryReviveDeadTarget(Entity<BibleComponent> bible, EntityUid user, EntityUid target, UseDelayComponent useDelay)
        {
            var (uid, component) = bible;

            if (component.ReviveDeadChance <= 0f)
                return;

            if (!HasComp<BibleUserComponent>(user))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-sizzle"), user, user);
                _audio.PlayPvs(component.SizzleSoundPath, user);
                _damageableSystem.TryChangeDamage(user, component.DamageOnUntrainedUse, true, origin: uid);
                _delay.TryResetDelay((uid, useDelay));
                return;
            }

            _delay.TryResetDelay((uid, useDelay));

            if (component.ReviveDeadOncePerBody && HasComp<BibleReviveAttemptedComponent>(target))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-revive-already-tried"), user, user, PopupType.MediumCaution);
                return;
            }

            if (component.ReviveDeadOncePerBody)
                EnsureComp<BibleReviveAttemptedComponent>(target);

            if (_rotting.IsRotten(target))
            {
                _popupSystem.PopupEntity(Loc.GetString("defibrillator-rotten"), user, user, PopupType.MediumCaution);
                _audio.PlayPvs(component.BibleHitSound, user);
                return;
            }

            if (TryComp<UnrevivableComponent>(target, out var unrevivable))
            {
                _popupSystem.PopupEntity(Loc.GetString(unrevivable.ReasonMessage), user, user, PopupType.MediumCaution);
                _audio.PlayPvs(component.BibleHitSound, user);
                return;
            }

            var userEnt = Identity.Entity(user, EntityManager);
            var targetEnt = Identity.Entity(target, EntityManager);

            if (!_random.Prob(component.ReviveDeadChance))
            {
                var othersFailMessage = Loc.GetString("bible-revive-fail-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
                var selfFailMessage = Loc.GetString("bible-revive-fail-self", ("target", targetEnt), ("bible", uid));

                _popupSystem.PopupEntity(othersFailMessage, user, Filter.PvsExcept(user), true, PopupType.SmallCaution);
                _popupSystem.PopupEntity(selfFailMessage, user, user, PopupType.MediumCaution);
                _audio.PlayPvs(component.BibleHitSound, user);
                return;
            }

            if (!TryComp<DamageableComponent>(target, out var damageable) ||
                !TryComp<MobThresholdsComponent>(target, out var thresholds) ||
                !_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var deadThreshold, thresholds))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-revive-fail-self", ("target", targetEnt), ("bible", uid)), user, user, PopupType.MediumCaution);
                _audio.PlayPvs(component.BibleHitSound, user);
                return;
            }

            var lethalThreshold = deadThreshold.Value;
            var desiredDamage = lethalThreshold * component.ReviveDeadDamageFraction;
            if (desiredDamage >= lethalThreshold)
                desiredDamage = lethalThreshold - FixedPoint2.Epsilon;

            var healAmount = damageable.TotalDamage - desiredDamage;
            if (healAmount > 0)
                _damageableSystem.HealDistributed((target, damageable), -healAmount, origin: uid);

            if (component.RestoreBloodOnRevive && TryComp<BloodstreamComponent>(target, out var bloodstream))
                _bloodstream.TryRegulateBloodLevel((target, bloodstream), bloodstream.BloodReferenceSolution.Volume);

            var revivedState = _mobStateSystem.HasState(target, MobState.Critical)
                ? MobState.Critical
                : MobState.Alive;
            _mobStateSystem.ChangeMobState(target, revivedState, origin: uid);

            ReturnSoulToBody(target);

            var othersMessage = Loc.GetString("bible-revive-success-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
            var selfMessage = Loc.GetString("bible-revive-success-self", ("target", targetEnt), ("bible", uid));

            _popupSystem.PopupEntity(othersMessage, user, Filter.PvsExcept(user), true, PopupType.Medium);
            _popupSystem.PopupEntity(selfMessage, user, user, PopupType.Large);
            _audio.PlayPvs(component.HealSoundPath, user);
        }

        private void ReturnSoulToBody(EntityUid target)
        {
            if (!_mind.TryGetMind(target, out var mindId, out var mind))
                return;

            if (mind.CurrentEntity == target)
                return;

            if (mind.VisitingEntity != null)
                _mind.UnVisit(mindId, mind);
            else
                _mind.TransferTo(mindId, target, ghostCheckOverride: true, mind: mind);
        }

        private void AddSummonVerb(EntityUid uid, SummonableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || component.AlreadySummoned || component.SpecialItemPrototype == null)
                return;

            if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(args.User))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    if (!TryComp(args.User, out TransformComponent? userXform))
                        return;

                    AttemptSummon((uid, component), args.User, userXform);
                },
                Text = Loc.GetString("bible-summon-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void GetSummonAction(EntityUid uid, SummonableComponent component, GetItemActionsEvent args)
        {
            if (component.AlreadySummoned)
                return;

            args.AddAction(ref component.SummonActionEntity, component.SummonAction);
        }

        private void OnSummon(Entity<SummonableComponent> ent, ref SummonActionEvent args)
        {
            AttemptSummon(ent, args.Performer, Transform(args.Performer));
        }

        /// <summary>
        /// Starts up the respawn stuff when
        /// the chaplain's familiar dies.
        /// </summary>
        private void OnFamiliarDeath(EntityUid uid, FamiliarComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState != MobState.Dead || component.Source == null)
                return;

            var source = component.Source;
            if (source != null && HasComp<SummonableComponent>(source))
            {
                _addQueue.Enqueue(source.Value);
            }
        }

        /// <summary>
        /// When the familiar spawns, set its source to the bible.
        /// </summary>
        private void OnSpawned(EntityUid uid, FamiliarComponent component, GhostRoleSpawnerUsedEvent args)
        {
            var parent = Transform(args.Spawner).ParentUid;
            if (!TryComp<SummonableComponent>(parent, out var summonable))
                return;

            component.Source = parent;
            summonable.Summon = uid;
        }

        private void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent? position)
        {
            var (uid, component) = ent;
            if (component.AlreadySummoned || component.SpecialItemPrototype == null)
                return;
            if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(user))
                return;
            if (!Resolve(user, ref position))
                return;
            if (component.Deleted || Deleted(uid))
                return;
            if (!_blocker.CanInteract(user, uid))
                return;

            // Make this familiar the component's summon
            var familiar = Spawn(component.SpecialItemPrototype, position.Coordinates);
            component.Summon = familiar;

            // If this is going to use a ghost role mob spawner, attach it to the bible.
            if (HasComp<GhostRoleMobSpawnerComponent>(familiar))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-summon-requested"), user, user, PopupType.Medium);
                _transform.SetParent(familiar, uid);
            }
            component.AlreadySummoned = true;
            _actionsSystem.RemoveAction(user, component.SummonActionEntity);
        }
    }
}
