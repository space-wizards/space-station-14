using Content.Server.Bible.Components;
using Content.Server.Chat; //Starlight
using Content.Server.Ghost.Roles.Events;
using Content.Server.Hands.Systems; //Starlight
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Bible;
using Content.Shared.Clumsy; //Starlight
using Content.Shared.Cluwne; //Starlight
using Content.Shared.Damage;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Hands.Components; //Starlight
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.Vampire.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedStunSystem _stun = default!;
        [Dependency] private readonly HandsSystem _hands = default!; //Starlight
        [Dependency] private readonly TagSystem _tags = default!; //Starlight

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BibleComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<BibleComponent, EntGotInsertedIntoContainerMessage>(OnInsertedContainer);
            SubscribeLocalEvent<SummonableComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
            SubscribeLocalEvent<SummonableComponent, GetItemActionsEvent>(GetSummonAction);
            SubscribeLocalEvent<SummonableComponent, SummonActionEvent>(OnSummon);
            SubscribeLocalEvent<FamiliarComponent, MobStateChangedEvent>(OnFamiliarDeath);
            SubscribeLocalEvent<FamiliarComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        }

        private void OnInsertedContainer(EntityUid uid, BibleComponent component, EntGotInsertedIntoContainerMessage args)
        {
            //If an unholy creature picks up the bible, knock them down
            if (HasComp<UnholyComponent>(args.Container.Owner))
            {
                Timer.Spawn(500, () =>
                {
                    _stun.TryUpdateParalyzeDuration(args.Container.Owner, TimeSpan.FromSeconds(10));
                    _damageableSystem.TryChangeDamage(args.Container.Owner, component.DamageOnUnholyUse);
                    _audio.PlayPvs(component.SizzleSoundPath, args.Container.Owner);
                });
            }
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

            if (args.Target == null || args.Target == args.User || !_mobStateSystem.IsAlive(args.Target.Value))
            {
                return;
            }

            if (!HasComp<BibleUserComponent>(args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-sizzle"), args.User, args.User);

                _audio.PlayPvs(component.SizzleSoundPath, args.User);
                _damageableSystem.TryChangeDamage(args.User, component.DamageOnUntrainedUse, true, origin: uid);
                _delay.TryResetDelay((uid, useDelay));

                return;
            }

            //Damage unholy creatures
            if (HasComp<UnholyComponent>(args.Target))
            {
                _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageUnholy, true, origin: uid);

                var othersMessage = Loc.GetString(component.LocPrefix + "-damage-unholy-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString(component.LocPrefix + "-damage-unholy-self", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.LargeCaution);

                _delay.TryResetDelay((uid, useDelay));

                return;
            }

            // This only has a chance to fail if the target is not wearing anything on their head and is not a familiar..
            if (!_invSystem.TryGetSlotEntity(args.Target.Value, "head", out var _) && !HasComp<FamiliarComponent>(args.Target.Value))
            {
                if (_random.Prob(component.FailChance))
                {
                    var othersFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                    _popupSystem.PopupEntity(othersFailMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);

                    var selfFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-self", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                    _popupSystem.PopupEntity(selfFailMessage, args.User, args.User, PopupType.MediumCaution);

                    _audio.PlayPvs(component.BibleHitSound, args.User);
                    _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageOnFail, true, origin: uid);
                    _delay.TryResetDelay((uid, useDelay));
                    return;
                }
            }

            //#region Starlight
            if (TryComp<CluwneComponent>(args.Target, out var cluwne))
            {
                if ((!cluwne.Unremovable) && _random.Prob(component.CluwneCureChance))
                {
                    var target = args.Target.Value;
                    RemComp<CluwneComponent>(target);
                    RemComp<ClumsyComponent>(uid);
                    RemComp<AutoEmoteComponent>(uid);
                    if (TryComp<InventoryComponent>(target, out var inv))
                    {
                        var slots = _invSystem.GetSlotEnumerator((target, inv));
                        while (slots.NextItem(out var itemeuid, out var slot))
                        {
                            if (TryComp<UnremoveableComponent>(itemeuid, out _) && !_tags.HasTag(itemeuid, component.RemovableAnywaysTag))
                                continue;
                            _invSystem.TryUnequip(target, target, slot.Name, true, true, inventory: inv);
                        }

                    }
                    if (EntityManager.TryGetComponent<HandsComponent>(target, out var hands))
                    {
                        foreach (var hand in _hands.EnumerateHands((target, hands)))
                        {
                            _hands.TryDrop(target,
                                hand,
                                checkActionBlocker: false,
                                doDropInteraction: false
                            );
                        }
                    }
                }
            }
            //#endregion

            var damage = _damageableSystem.TryChangeDamage(args.Target.Value, component.Damage, true, origin: uid);

            if (damage == null || damage.Empty)
            {
                var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

                var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-self", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
            }
            else
            {
                var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

                var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-self", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
                _audio.PlayPvs(component.HealSoundPath, args.User);
                _delay.TryResetDelay((uid, useDelay));
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
