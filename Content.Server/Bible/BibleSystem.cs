using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.MobState.Components;
using Content.Shared.MobState;
using Content.Shared.Damage;
using Content.Shared.Verbs;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Server.Cooldown;
using Content.Server.Bible.Components;
using Content.Server.Popups;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Bible
{
    public sealed class BibleSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _invSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BibleComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<SummonableComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
            SubscribeLocalEvent<SummonableComponent, GetItemActionsEvent>(GetSummonAction);
            SubscribeLocalEvent<SummonableComponent, SummonActionEvent>(OnSummon);
            SubscribeLocalEvent<FamiliarComponent, MobStateChangedEvent>(OnFamiliarDeath);
        }

        private Queue<EntityUid> AddQueue = new();
        private Queue<EntityUid> RemQueue = new();

        /// <summary>
        /// This handles familiar respawning.
        /// </summary>
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach(var entity in AddQueue)
            {
                EnsureComp<SummonableRespawningComponent>(entity);
            }
            AddQueue.Clear();

            foreach(var entity in RemQueue)
            {
                RemComp<SummonableRespawningComponent>(entity);
            }
            RemQueue.Clear();

            foreach (var (respawning, summonableComp) in EntityQuery<SummonableRespawningComponent, SummonableComponent>())
            {
                summonableComp.Accumulator += frameTime;
                if (summonableComp.Accumulator < summonableComp.RespawnTime)
                {
                    continue;
                }
                /// Clean up the old body
                if (summonableComp.Summon != null)
                {
                    EntityManager.DeleteEntity(summonableComp.Summon.Value);
                    summonableComp.Summon = null;
                }
                summonableComp.AlreadySummoned = false;
                _popupSystem.PopupEntity(Loc.GetString("bible-summon-respawn-ready", ("book", summonableComp.Owner)), summonableComp.Owner, Filter.Pvs(summonableComp.Owner));
                SoundSystem.Play("/Audio/Effects/radpulse9.ogg", Filter.Pvs(summonableComp.Owner), summonableComp.Owner, AudioParams.Default.WithVolume(-4f));
                /// Clean up the accumulator and respawn tracking component
                summonableComp.Accumulator = 0;
                RemQueue.Enqueue(respawning.Owner);
            }
        }

        private void OnAfterInteract(EntityUid uid, BibleComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            var currentTime = _gameTiming.CurTime;

            if (currentTime < component.CooldownEnd)
            {
                return;
            }
            
            if (args.Target == null || args.Target == args.User || !TryComp<MobStateComponent>(args.Target, out var mobState)
                || mobState.IsDead())
            {
                return;
            }

            component.LastAttackTime = currentTime;
            component.CooldownEnd = component.LastAttackTime + TimeSpan.FromSeconds(component.CooldownTime);
            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(component.LastAttackTime, component.CooldownEnd), false);

            if (!HasComp<BibleUserComponent>(args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-sizzle"), args.User, Filter.Entities(args.User));

                SoundSystem.Play(component.SizzleSoundPath.GetSound(), Filter.Pvs(args.User), args.User);
                _damageableSystem.TryChangeDamage(args.User, component.DamageOnUntrainedUse, true);

                return;
            }

            // This only has a chance to fail if the target is not wearing anything on their head and is not a familiar.
            if (!_invSystem.TryGetSlotEntity(args.Target.Value, "head", out var _) && !HasComp<FamiliarComponent>(args.Target.Value))
            {
                if (_random.Prob(component.FailChance))
                {
                    var othersFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-others", ("user", args.User),("target", args.Target),("bible", uid));
                    _popupSystem.PopupEntity(othersFailMessage, args.User, Filter.Pvs(args.User).RemoveWhereAttachedEntity(puid => puid == args.User));

                    var selfFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-self", ("target", args.Target),("bible", uid));
                    _popupSystem.PopupEntity(selfFailMessage, args.User, Filter.Entities(args.User));

                    SoundSystem.Play("/Audio/Effects/hit_kick.ogg", Filter.Pvs(args.Target.Value), args.User);
                    _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageOnFail, true);
                    return;
                }
            }

            var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-others", ("user", args.User),("target", args.Target),("bible", uid));
            _popupSystem.PopupEntity(othersMessage, args.User, Filter.Pvs(args.User).RemoveWhereAttachedEntity(puid => puid == args.User));

            var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-self", ("target", args.Target),("bible", uid));
            _popupSystem.PopupEntity(selfMessage, args.User, Filter.Entities(args.User));

            SoundSystem.Play(component.HealSoundPath.GetSound(), Filter.Pvs(args.Target.Value), args.User);
            _damageableSystem.TryChangeDamage(args.Target.Value, component.Damage, true);
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
                    TransformComponent? position = Comp<TransformComponent>(args.User);
                    AttemptSummon(component, args.User, position);
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

            args.Actions.Add(component.SummonAction);
        }
        private void OnSummon(EntityUid uid, SummonableComponent component, SummonActionEvent args)
        {
            AttemptSummon(component, args.Performer, Transform(args.Performer));
        }

        /// <summary>
        /// Starts up the respawn stuff when
        /// the chaplain's familiar dies.
        /// </summary>
        private void OnFamiliarDeath(EntityUid uid, FamiliarComponent component, MobStateChangedEvent args)
        {
            if (!args.Component.IsDead() || component.Source == null)
                return;

            var source = component.Source;
            if (source != null && TryComp<SummonableComponent>(source, out var summonable))
            {
                AddQueue.Enqueue(summonable.Owner);
            }
        }


        private void AttemptSummon(SummonableComponent component, EntityUid user, TransformComponent? position)
        {
            if (component.AlreadySummoned || component.SpecialItemPrototype == null)
                return;
            if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(user))
                return;
            if (!Resolve(user, ref position))
                return;
            if (component.Deleted || Deleted(component.Owner))
                return;
            if (!_blocker.CanInteract(user, component.Owner))
                return;

            // Make this familiar the component's summon
            var familiar = EntityManager.SpawnEntity(component.SpecialItemPrototype, position.Coordinates);
                            component.Summon = familiar;

            /// We only want to add the familiar component to mobs
            if (HasComp<MobStateComponent>(familiar))
            {
                /// Make this Summon the familiar's source
                var familiarComp = EnsureComp<FamiliarComponent>(familiar);
                familiarComp.Source = component.Owner;
            }
            component.AlreadySummoned = true;
            _actionsSystem.RemoveAction(user, component.SummonAction);
        }
    }

    public sealed class SummonActionEvent : InstantActionEvent
    {}
}
