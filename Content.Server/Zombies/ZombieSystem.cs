using Content.Shared.NPC.Prototypes;
using Content.Server.Actions;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Armor;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cloning.Events;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Revolutionary;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Ghost.Roles.Components;

namespace Content.Server.Zombies
{
    public sealed partial class ZombieSystem : SharedZombieSystem
    {
        private static readonly EntityTimerId InfectionTimer = new("infection");
        private static readonly EntityTimerId HealingTimer = new("healing");

        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private BloodstreamSystem _bloodstream = default!;
        [Dependency] private DamageableSystem _damageable = default!;
        [Dependency] private ChatSystem _chat = default!;
        [Dependency] private ActionsSystem _actions = default!;
        [Dependency] private AutoEmoteSystem _autoEmote = default!;
        [Dependency] private EmoteOnDamageSystem _emoteOnDamage = default!;
        [Dependency] private MobStateSystem _mobState = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private SharedRoleSystem _role = default!;
        [Dependency] private EntityTimerSystem _timers = default!;

        public readonly ProtoId<NpcFactionPrototype> Faction = "Zombie";

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
            SubscribeLocalEvent<ZombieComponent, AttemptConvertRevolutionaryEvent>(OnAttemptConvert);

            SubscribeLocalEvent<PendingZombieComponent, MapInitEvent>(OnPendingMapInit);
            SubscribeLocalEvent<PendingZombieComponent, BeforeRemoveAnomalyOnDeathEvent>(OnBeforeRemoveAnomalyOnDeath);
            SubscribeLocalEvent<PendingZombieComponent, EntityTimerEvent>(OnInfectionTimer);
            SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnZombieStartup);
            SubscribeLocalEvent<ZombieComponent, EntityTimerEvent>(OnHealingTimer);

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
            _faction.AddFaction(uid, Faction);

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
            _timers.SetTimerAt<PendingZombieComponent>((uid, component), InfectionTimer, component.NextTick);
        }

        private void OnInfectionTimer(Entity<PendingZombieComponent> ent, ref EntityTimerEvent args)
        {
            if (args.Id != InfectionTimer ||
                !TryComp<Shared.Damage.Components.DamageableComponent>(ent, out var damage) ||
                !TryComp<MobStateComponent>(ent, out var mobState))
                return;

            var comp = ent.Comp;
            comp.NextTick = args.FiredAt + TimeSpan.FromSeconds(1);
            _timers.SetTimerAt(ent, InfectionTimer, comp.NextTick);

            comp.GracePeriod -= TimeSpan.FromSeconds(1);
            if (comp.GracePeriod > TimeSpan.Zero)
                return;

            if (_random.Prob(comp.InfectionWarningChance))
                _popup.PopupEntity(Loc.GetString(_random.Pick(comp.InfectionWarnings)), ent, ent);

            var multiplier = _mobState.IsCritical(ent, mobState)
                ? comp.CritDamageMultiplier
                : 1f;

            _damageable.ChangeDamage((ent.Owner, damage), comp.Damage * multiplier, true, false);
        }

        private void OnZombieStartup(Entity<ZombieComponent> ent, ref ComponentStartup args)
        {
            ent.Comp.NextTick = _timing.CurTime;
            _timers.SetTimer(ent, HealingTimer, TimeSpan.FromSeconds(1));
        }

        private void OnHealingTimer(Entity<ZombieComponent> ent, ref EntityTimerEvent args)
        {
            if (args.Id != HealingTimer ||
                !TryComp<Shared.Damage.Components.DamageableComponent>(ent, out var damage) ||
                !TryComp<MobStateComponent>(ent, out var mobState))
                return;

            ent.Comp.NextTick = args.FiredAt;
            _timers.SetTimer(ent, HealingTimer, TimeSpan.FromSeconds(1));

            if (_mobState.IsDead(ent, mobState))
                return;

            var multiplier = _mobState.IsCritical(ent, mobState)
                ? ent.Comp.PassiveHealingCritMultiplier
                : 1f;

            _damageable.ChangeDamage((ent.Owner, damage), ent.Comp.PassiveHealing * multiplier, true, false);
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

        private void OnEmote(EntityUid uid, ZombieComponent component, ref EmoteEvent args)
        {
            // always play zombie emote sounds and ignore others
            if (args.Handled)
                return;

            ProtoMan.Resolve(component.EmoteSoundsId, out var sounds);

            args.Handled = _chat.TryPlayEmoteSound(uid, sounds, args.Emote);
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

        private void OnMeleeHit(Entity<ZombieComponent> entity, ref MeleeHitEvent args)
        {
            if (!args.IsHit)
                return;

            var cannotSpread = HasComp<NonSpreaderZombieComponent>(args.User);

            foreach (var uid in args.HitEntities)
            {
                if (args.User == uid)
                    continue;

                if (!TryComp<MobStateComponent>(uid, out var mobState))
                    continue;

                if (HasComp<ZombieComponent>(uid) || HasComp<IncurableZombieComponent>(uid))
                {
                    // Don't infect, don't deal damage, do not heal from bites, don't pass go!
                    args.Handled = true;
                    continue;
                }

                if (_mobState.IsAlive(uid, mobState))
                {
                    _damageable.TryChangeDamage(args.User, entity.Comp.HealingOnBite, true, false);

                    // If we cannot infect the living target, the zed will just heal itself.
                    if (HasComp<ZombieImmuneComponent>(uid) || cannotSpread || !_random.Prob(GetZombieInfectionChance(uid, entity.Comp)))
                        continue;

                    EnsureComp<PendingZombieComponent>(uid);
                    EnsureComp<ZombifyOnDeathComponent>(uid);
                }
                else
                {
                    if (HasComp<ZombieImmuneComponent>(uid) || cannotSpread)
                        continue;

                    // If the target is dead and can be infected, infect.
                    ZombifyEntity(uid);
                    args.Handled = true;
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

            _visualBody.ApplyProfiles(target, zombiecomp.BeforeZombifiedProfiles);
            _visualBody.ApplyMarkings(target, zombiecomp.BeforeZombifiedMarkings);

            _bloodstream.ChangeBloodReagents(target, zombiecomp.BeforeZombifiedBloodReagents);

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
        // We also need to make sure the zombie is a ghost role because zombies with minds do not get a ghostrolecomponent
        private void OnMindRemoved(Entity<ZombieComponent> ent, ref MindRemovedMessage args)
        {
            _role.MindRemoveRole<ZombieRoleComponent>((args.Mind.Owner,  args.Mind.Comp));
            MakeGhostRole(ent.Owner);
        }

        private void OnAttemptConvert(Entity<ZombieComponent> ent, ref AttemptConvertRevolutionaryEvent args)
        {
            args.Cancelled = true;
        }

        /// <summary>
        /// Makes the target entity a zombie ghost role. Should only be fired when the entity does not have a mind.
        /// </summary>
        private void MakeGhostRole(EntityUid ent)
        {
            //yet more hardcoding. Visit zombie.ftl for more information.
            var ghostRole = EnsureComp<GhostRoleComponent>(ent);
            EnsureComp<GhostTakeoverAvailableComponent>(ent);

            ghostRole.RoleName = Loc.GetString("zombie-generic");
            ghostRole.RoleDescription = Loc.GetString("zombie-role-desc");
            ghostRole.RoleRules = Loc.GetString("zombie-role-rules");
            ghostRole.MindRoles.Add(MindRoleZombie);
        }
    }
}
