using System.Linq;
using Content.Server.Actions;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Server.Roles;
using Content.Shared.Anomaly.Components;
using Content.Shared.Armor;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cloning.Events;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Zombies
{
    public sealed partial class ZombieSystem : SharedZombieSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
        [Dependency] private readonly EmoteOnDamageSystem _emoteOnDamage = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedRoleSystem _role = default!;

        public const SlotFlags ProtectiveSlots =
            SlotFlags.FEET |
            SlotFlags.HEAD |
            SlotFlags.EYES |
            SlotFlags.GLOVES |
            SlotFlags.MASK |
            SlotFlags.NECK |
            SlotFlags.INNERCLOTHING |
            SlotFlags.OUTERCLOTHING;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<ZombieComponent, EmoteEvent>(OnEmote, before:
                new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });

            SubscribeLocalEvent<ZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ZombieComponent, MobStateChangedEvent>(OnMobState);
            SubscribeLocalEvent<ZombieComponent, CloningEvent>(OnZombieCloning);
            SubscribeLocalEvent<ZombieComponent, TryingToSleepEvent>(OnSleepAttempt);
            SubscribeLocalEvent<ZombieComponent, GetCharactedDeadIcEvent>(OnGetCharacterDeadIC);
            SubscribeLocalEvent<ZombieComponent, GetCharacterUnrevivableIcEvent>(OnGetCharacterUnrevivableIC);
            SubscribeLocalEvent<ZombieComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<ZombieComponent, MindRemovedMessage>(OnMindRemoved);

            SubscribeLocalEvent<PendingZombieComponent, MapInitEvent>(OnPendingMapInit);
            SubscribeLocalEvent<PendingZombieComponent, BeforeRemoveAnomalyOnDeathEvent>(OnBeforeRemoveAnomalyOnDeath);

            SubscribeLocalEvent<IncurableZombieComponent, MapInitEvent>(OnPendingMapInit);

            SubscribeLocalEvent<ZombifyOnDeathComponent, MobStateChangedEvent>(OnDamageChanged);
        }

        private void OnBeforeRemoveAnomalyOnDeath(Entity<PendingZombieComponent> ent, ref BeforeRemoveAnomalyOnDeathEvent args)
        {
            // Pending zombies (e.g. infected non-zombies) do not remove their hosted anomaly on death.
            // Current zombies DO remove the anomaly on death.
            args.Cancelled = true;
        }

        private void OnPendingMapInit(EntityUid uid, IncurableZombieComponent component, MapInitEvent args)
        {
            _actions.AddAction(uid, ref component.Action, component.ZombifySelfActionPrototype);

            if (HasComp<ZombieComponent>(uid) || HasComp<ZombieImmuneComponent>(uid))
                return;

            EnsureComp<PendingZombieComponent>(uid, out PendingZombieComponent pendingComp);

            pendingComp.GracePeriod = _random.Next(pendingComp.MinInitialInfectedGrace, pendingComp.MaxInitialInfectedGrace);
        }

        private void OnPendingMapInit(EntityUid uid, PendingZombieComponent component, MapInitEvent args)
        {
            if (_mobState.IsDead(uid))
            {
                ZombifyEntity(uid);
                return;
            }

            component.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1f);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var curTime = _timing.CurTime;

            // Hurt the living infected
            var query = EntityQueryEnumerator<PendingZombieComponent, DamageableComponent, MobStateComponent>();
            while (query.MoveNext(out var uid, out var comp, out var damage, out var mobState))
            {
                // Process only once per second
                if (comp.NextTick > curTime)
                    continue;

                comp.NextTick = curTime + TimeSpan.FromSeconds(1f);

                comp.GracePeriod -= TimeSpan.FromSeconds(1f);
                if (comp.GracePeriod > TimeSpan.Zero)
                    continue;

                if (_random.Prob(comp.InfectionWarningChance))
                    _popup.PopupEntity(Loc.GetString(_random.Pick(comp.InfectionWarnings)), uid, uid);

                var multiplier = _mobState.IsCritical(uid, mobState)
                    ? comp.CritDamageMultiplier
                    : 1f;

                _damageable.TryChangeDamage(uid, comp.Damage * multiplier, true, false, damage);
            }

            // Heal the zombified
            var zombQuery = EntityQueryEnumerator<ZombieComponent, DamageableComponent, MobStateComponent>();
            while (zombQuery.MoveNext(out var uid, out var comp, out var damage, out var mobState))
            {
                // Process only once per second
                if (comp.NextTick + TimeSpan.FromSeconds(1) > curTime)
                    continue;

                comp.NextTick = curTime;

                if (_mobState.IsDead(uid, mobState))
                    continue;

                var multiplier = _mobState.IsCritical(uid, mobState)
                    ? comp.PassiveHealingCritMultiplier
                    : 1f;

                // Gradual healing for living zombies.
                _damageable.TryChangeDamage(uid, comp.PassiveHealing * multiplier, true, false, damage);
            }
        }

        private void OnSleepAttempt(EntityUid uid, ZombieComponent component, ref TryingToSleepEvent args)
        {
            args.Cancelled = true;
        }

        private void OnGetCharacterDeadIC(EntityUid uid, ZombieComponent component, ref GetCharactedDeadIcEvent args)
        {
            args.Dead = true;
        }

        private void OnGetCharacterUnrevivableIC(EntityUid uid, ZombieComponent component, ref GetCharacterUnrevivableIcEvent args)
        {
            args.Unrevivable = true;
        }

        private void OnStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
        {
            if (component.EmoteSoundsId == null)
                return;
            _protoManager.TryIndex(component.EmoteSoundsId, out component.EmoteSounds);
        }

        private void OnEmote(EntityUid uid, ZombieComponent component, ref EmoteEvent args)
        {
            // always play zombie emote sounds and ignore others
            if (args.Handled)
                return;
            args.Handled = _chat.TryPlayEmoteSound(uid, component.EmoteSounds, args.Emote);
        }

        private void OnMobState(EntityUid uid, ZombieComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Alive)
            {
                // Groaning when damaged
                EnsureComp<EmoteOnDamageComponent>(uid);
                _emoteOnDamage.AddEmote(uid, "Scream");

                // Random groaning
                EnsureComp<AutoEmoteComponent>(uid);
                _autoEmote.AddEmote(uid, "ZombieGroan");
            }
            else
            {
                // Stop groaning when damaged
                _emoteOnDamage.RemoveEmote(uid, "Scream");

                // Stop random groaning
                _autoEmote.RemoveEmote(uid, "ZombieGroan");
            }
        }

        private float GetZombieInfectionChance(EntityUid uid, ZombieComponent zombieComponent)
        {
            var chance = zombieComponent.BaseZombieInfectionChance;

            var armorEv = new CoefficientQueryEvent(ProtectiveSlots);
            RaiseLocalEvent(uid, armorEv);
            foreach (var resistanceEffectiveness in zombieComponent.ResistanceEffectiveness.DamageDict)
            {
                if (armorEv.DamageModifiers.Coefficients.TryGetValue(resistanceEffectiveness.Key, out var coefficient))
                {
                    // Scale the coefficient by the resistance effectiveness, very descriptive I know
                    // For example. With 30% slash resist (0.7 coeff), but only a 60% resistance effectiveness for slash,
                    // you'll end up with 1 - (0.3 * 0.6) = 0.82 coefficient, or a 18% resistance
                    var adjustedCoefficient = 1 - ((1 - coefficient) * resistanceEffectiveness.Value.Float());
                    chance *= adjustedCoefficient;
                }
            }

            var zombificationResistanceEv = new ZombificationResistanceQueryEvent(ProtectiveSlots);
            RaiseLocalEvent(uid, zombificationResistanceEv);
            chance *= zombificationResistanceEv.TotalCoefficient;

            return MathF.Max(chance, zombieComponent.MinZombieInfectionChance);
        }

        private void OnMeleeHit(EntityUid uid, ZombieComponent component, MeleeHitEvent args)
        {
            if (!TryComp<ZombieComponent>(args.User, out _))
                return;

            if (!args.HitEntities.Any())
                return;

            foreach (var entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (!TryComp<MobStateComponent>(entity, out var mobState))
                    continue;

                if (HasComp<ZombieComponent>(entity))
                {
                    args.BonusDamage = -args.BaseDamage;
                }
                else
                {
                    if (!HasComp<ZombieImmuneComponent>(entity) && !HasComp<NonSpreaderZombieComponent>(args.User) && _random.Prob(GetZombieInfectionChance(entity, component)))
                    {
                        EnsureComp<PendingZombieComponent>(entity);
                        EnsureComp<ZombifyOnDeathComponent>(entity);
                    }
                }

                if (_mobState.IsIncapacitated(entity, mobState) && !HasComp<ZombieComponent>(entity) && !HasComp<ZombieImmuneComponent>(entity))
                {
                    ZombifyEntity(entity);
                    args.BonusDamage = -args.BaseDamage;
                }
                else if (mobState.CurrentState == MobState.Alive) //heals when zombies bite live entities
                {
                    _damageable.TryChangeDamage(uid, component.HealingOnBite, true, false);
                }
            }
        }

        /// <summary>
        ///     This is the function to call if you want to unzombify an entity.
        /// </summary>
        /// <param name="source">the entity having the ZombieComponent</param>
        /// <param name="target">the entity you want to unzombify (different from source in case of cloning, for example)</param>
        /// <param name="zombiecomp"></param>
        /// <remarks>
        ///     this currently only restore the skin/eye color from before zombified
        ///     TODO: completely rethink how zombies are done to allow reversal.
        /// </remarks>
        public bool UnZombify(EntityUid source, EntityUid target, ZombieComponent? zombiecomp)
        {
            if (!Resolve(source, ref zombiecomp))
                return false;

            foreach (var (layer, info) in zombiecomp.BeforeZombifiedCustomBaseLayers)
            {
                _humanoidAppearance.SetBaseLayerColor(target, layer, info.Color);
                _humanoidAppearance.SetBaseLayerId(target, layer, info.Id);
            }
            if (TryComp<HumanoidAppearanceComponent>(target, out var appcomp))
            {
                appcomp.EyeColor = zombiecomp.BeforeZombifiedEyeColor;
            }
            _humanoidAppearance.SetSkinColor(target, zombiecomp.BeforeZombifiedSkinColor, false);
            _bloodstream.ChangeBloodReagent(target, zombiecomp.BeforeZombifiedBloodReagent);

            return true;
        }

        private void OnZombieCloning(Entity<ZombieComponent> ent, ref CloningEvent args)
        {
            UnZombify(ent.Owner, args.CloneUid, ent.Comp);
        }

        // Make sure players that enter a zombie (for example via a ghost role or the mind swap spell) count as an antagonist.
        private void OnMindAdded(Entity<ZombieComponent> ent, ref MindAddedMessage args)
        {
            if (!_role.MindHasRole<ZombieRoleComponent>(args.Mind))
                _role.MindAddRole(args.Mind, "MindRoleZombie", mind: args.Mind.Comp);
        }

        // Remove the role when getting cloned, getting gibbed and borged, or leaving the body via any other method.
        private void OnMindRemoved(Entity<ZombieComponent> ent, ref MindRemovedMessage args)
        {
            _role.MindRemoveRole<ZombieRoleComponent>((args.Mind.Owner,  args.Mind.Comp));
        }
    }
}
