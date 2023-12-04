using Content.Server.Bible.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Bible;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Bible
{
    public sealed class BibleSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly InventorySystem _invSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly UseDelaySystem _delay = default!;

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

            foreach(var entity in _addQueue)
            {
                EnsureComp<SummonableRespawningComponent>(entity);
            }
            _addQueue.Clear();

            foreach(var entity in _remQueue)
            {
                RemComp<SummonableRespawningComponent>(entity);
            }
            _remQueue.Clear();

            var query = EntityQueryEnumerator<SummonableRespawningComponent, SummonableComponent>();
            while (query.MoveNext(out var uid, out var respawning, out var summonableComp))
            {
                summonableComp.Accumulator += frameTime;
                if (summonableComp.Accumulator < summonableComp.RespawnTime)
                {
                    continue;
                }
                // Clean up the old body
                if (summonableComp.Summon != null)
                {
                    EntityManager.DeleteEntity(summonableComp.Summon.Value);
                    summonableComp.Summon = null;
                }
                summonableComp.AlreadySummoned = false;
                _popupSystem.PopupEntity(Loc.GetString("bible-summon-respawn-ready", ("book", summonableComp.Owner)), summonableComp.Owner, PopupType.Medium);
                _audio.PlayPvs("/Audio/Effects/radpulse9.ogg", summonableComp.Owner, AudioParams.Default.WithVolume(-4f));
                // Clean up the accumulator and respawn tracking component
                summonableComp.Accumulator = 0;
                _remQueue.Enqueue(uid);
            }
        }

        private void OnAfterInteract(EntityUid uid, BibleComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            UseDelayComponent? delay = null;

            if (_delay.ActiveDelay(uid, delay))
                return;

            if (args.Target == null || args.Target == args.User || !_mobStateSystem.IsAlive(args.Target.Value))
            {
                return;
            }

            if (!HasComp<BibleUserComponent>(args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-sizzle"), args.User, args.User);

                _audio.PlayPvs(component.SizzleSoundPath, args.User);
                _damageableSystem.TryChangeDamage(args.User, component.DamageOnUntrainedUse, true, origin: uid);
                _delay.BeginDelay(uid, delay);

                return;
            }

            // This only has a chance to fail if the target is not wearing anything on their head and is not a familiar.
            if (!_invSystem.TryGetSlotEntity(args.Target.Value, "head", out var _) && !HasComp<FamiliarComponent>(args.Target.Value))
            {
                if (_random.Prob(component.FailChance))
                {
                    var othersFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-others", ("user", Identity.Entity(args.User, EntityManager)),("target", Identity.Entity(args.Target.Value, EntityManager)),("bible", uid));
                    _popupSystem.PopupEntity(othersFailMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);

                    var selfFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-self", ("target", Identity.Entity(args.Target.Value, EntityManager)),("bible", uid));
                    _popupSystem.PopupEntity(selfFailMessage, args.User, args.User, PopupType.MediumCaution);

                    _audio.PlayPvs("/Audio/Effects/hit_kick.ogg", args.User);
                    _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageOnFail, true, origin: uid);
                    _delay.BeginDelay(uid, delay);
                    return;
                }
            }

            var damage = _damageableSystem.TryChangeDamage(args.Target.Value, component.Damage, true, origin: uid);

            if (damage == null || damage.Empty)
            {
                var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-others", ("user", Identity.Entity(args.User, EntityManager)),("target", Identity.Entity(args.Target.Value, EntityManager)),("bible", uid));
                _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

                var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-self", ("target", Identity.Entity(args.Target.Value, EntityManager)),("bible", uid));
                _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
            }
            else
            {
                var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-others", ("user", Identity.Entity(args.User, EntityManager)),("target", Identity.Entity(args.Target.Value, EntityManager)),("bible", uid));
                _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

                var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-self", ("target", Identity.Entity(args.Target.Value, EntityManager)),("bible", uid));
                _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
                _audio.PlayPvs(component.HealSoundPath, args.User);
                _delay.BeginDelay(uid, delay);
            }
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
                    if (!TryComp<TransformComponent>(args.User, out var userXform)) return;

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
            var familiar = EntityManager.SpawnEntity(component.SpecialItemPrototype, position.Coordinates);
                            component.Summon = familiar;

            // If this is going to use a ghost role mob spawner, attach it to the bible.
            if (HasComp<GhostRoleMobSpawnerComponent>(familiar))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-summon-requested"), user, PopupType.Medium);
                Transform(familiar).AttachParent(uid);
            }
            component.AlreadySummoned = true;
            _actionsSystem.RemoveAction(user, component.SummonActionEntity);
        }
    }
}
