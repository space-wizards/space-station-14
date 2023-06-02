using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Cloning;
using Content.Server.Drone.Components;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chemistry.Components;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Zombies
{
    public sealed class ZombieSystem : SharedZombieSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;
        [Dependency] private readonly ServerInventorySystem _inv = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
        [Dependency] private readonly EmoteOnDamageSystem _emoteOnDamage = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieComponent, EmoteEvent>(OnEmote, before:
                new []{typeof(VocalSystem), typeof(BodyEmotesSystem)});

            SubscribeLocalEvent<ZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ZombieComponent, MobStateChangedEvent>(OnMobState);
            SubscribeLocalEvent<ZombieComponent, CloningEvent>(OnZombieCloning);
            SubscribeLocalEvent<ZombieComponent, TryingToSleepEvent>(OnSleepAttempt);

            SubscribeLocalEvent<PendingZombieComponent, MapInitEvent>(OnPendingMapInit);
            SubscribeLocalEvent<PendingZombieComponent, MobStateChangedEvent>(OnPendingMobState);
        }

        private void OnPendingMapInit(EntityUid uid, PendingZombieComponent component, MapInitEvent args)
        {
            component.NextTick = _timing.CurTime;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var query = EntityQueryEnumerator<PendingZombieComponent, DamageableComponent, MobStateComponent>();
            var curTime = _timing.CurTime;

            var zombQuery = EntityQueryEnumerator<ZombieComponent, DamageableComponent, MobStateComponent>();

            // Hurt the living infected
            while (query.MoveNext(out var uid, out var comp, out var damage, out var mobState))
            {
                // Process only once per second
                if (comp.NextTick + TimeSpan.FromSeconds(1) > curTime)
                    continue;

                comp.NextTick = curTime;

                comp.InfectedSecs += 1;
                // See if there should be a warning popup for the player.
                if (comp.InfectionWarnings.TryGetValue(comp.InfectedSecs, out var popupStr))
                {
                    _popup.PopupEntity(Loc.GetString(popupStr), uid, uid);
                }

                if (comp.InfectedSecs < 0)
                {
                    // This zombie has a latent virus, probably set up by ZombieRuleSystem. No damage yet.
                    continue;
                }
                if (mobState.CurrentState == MobState.Dead)
                {
                    if (comp.InfectedSecs >= comp.Settings.ZombieDeadMinTurnTime)
                    {
                        // You can turn into a zombie now.
                        _zombify.ZombifyEntity(uid, mobState, pending);
                    }

                    // No need to compute extra damage if you are already dead.
                    continue;
                }

                // Pain of becoming a zombie grows over time
                // By scaling the number of seconds we have an accessible way to scale this exponential function.
                //   The function was hand tuned to 120 seconds, hence the 120 constant here.
                var scaledSeconds = (120.0f / comp.MaxInfectionLength) * comp.InfectedSecs;

                // 1x at 30s, 3x at 60s, 6x at 90s, 10x at 120s. Limit at 20x so we don't gib you.
                var painMultiple = Math.Min(20f, 0.1f + 0.02f * scaledSeconds + 0.0005f * scaledSeconds * scaledSeconds);
                if (mobState.CurrentState == MobState.Critical)
                {
                    // Speed up their transformation when they are (or have been) in crit by ensuring their damage
                    //   multiplier is at least 10x
                    painMultiple = Math.Max(comp.MinimumCritMultiplier, painMultiple);
                }
                _damageable.TryChangeDamage(uid, comp.VirusDamage * painMultiple, true, false, damage);
            }

            // Heal the zombified
            while (zombQuery.MoveNext(out var uid, out var comp, out var damage, out var mobState))
            {
                // Process only once per second
                if (comp.NextTick + TimeSpan.FromSeconds(1) > curTime)
                    continue;

                comp.NextTick = curTime;

                if (comp.Permadeath)
                {
                    // No healing
                    continue;
                }

                if (mobState.CurrentState == MobState.Alive)
                {
                    // Gradual healing for living zombies.
                    _damageable.TryChangeDamage(uid, comp.Healing, true, false, damage);
                }
                else if (_random.Prob(comp.ZombieReviveChance))
                {
                    // There's a small chance to reverse most of the zombie's damage (damage.Damage) in one go
                    var multiplier = Math.Max(0.7f, (float)((damage.TotalDamage - 50.0f) / damage.TotalDamage));
                    _damageable.TryChangeDamage(uid, -damage.Damage * multiplier, true, false, damage);
                }
            }
        }

        private void OnSleepAttempt(EntityUid uid, ZombieComponent component, ref TryingToSleepEvent args)
        {
            args.Cancelled = true;
        }

        private void OnEmote(EntityUid uid, ZombieComponent component, ref EmoteEvent args)
        {
            // always play zombie emote sounds and ignore others
            if (args.Handled)
                return;
            args.Handled = _chat.TryPlayEmoteSound(uid, component.Settings.EmoteSounds, args.Emote);
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

                // Roll to see if this zombie is not coming back.
                //   Note that due to damage reductions it takes a lot of hits to gib a zombie without this.
                if (_random.Prob((args.NewMobState == MobState.Dead)? component.ZombiePermadeathChance : component.ZombieCritDeathChance))
                {
                    // You're dead! No reviving for you.
                    _mobThreshold.SetAllowRevives(uid, false);
                    component.Permadeath = true;
                    _popup.PopupEntity(Loc.GetString("zombie-permadeath"), uid, uid);
                }
            }
        }

        private void OnPendingMobState(EntityUid uid, PendingZombieComponent pending, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Critical)
            {
                // Immediately jump to an active virus when you crit
                pending.InfectedSecs = Math.Max(0, pending.InfectedSecs);
            }
        }

        private float GetZombieInfectionChance(EntityUid uid, ZombieComponent component)
        {
            var baseChance = component.MaxZombieInfectionChance;

            if (!TryComp<InventoryComponent>(uid, out var inventoryComponent))
                return baseChance;

            var enumerator =
                new InventorySystem.ContainerSlotEnumerator(uid, inventoryComponent.TemplateId, _protoManager, _inv,
                    SlotFlags.FEET |
                    SlotFlags.HEAD |
                    SlotFlags.EYES |
                    SlotFlags.GLOVES |
                    SlotFlags.MASK |
                    SlotFlags.NECK |
                    SlotFlags.INNERCLOTHING |
                    SlotFlags.OUTERCLOTHING);

            var items = 0f;
            var total = 0f;
            while (enumerator.MoveNext(out var con))
            {
                total++;

                if (con.ContainedEntity != null)
                    items++;
            }

            var max = component.MaxZombieInfectionChance;
            var min = component.MinZombieInfectionChance;
            //gets a value between the max and min based on how many items the entity is wearing
            var chance = (max-min) * ((total - items)/total) + min;
            return chance;
        }

        private void OnMeleeHit(EntityUid uid, ZombieComponent component, MeleeHitEvent args)
        {
            if (!EntityManager.TryGetComponent<ZombieComponent>(args.User, out var zombieComp))
                return;

            if (!args.HitEntities.Any())
                return;

            foreach (var entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (!TryComp<MobStateComponent>(entity, out var mobState) || HasComp<DroneComponent>(entity))
                    continue;

                if (HasComp<ZombieComponent>(entity))
                {
                    args.BonusDamage = -args.BaseDamage * zombieComp.OtherZombieDamageCoefficient;
                    if (_random.Prob(0.3f))
                    {
                        // Tell the zombo that they are eating the dead
                        _popup.PopupEntity(Loc.GetString("zombie-bite-already-infected"), uid, uid);
                    }
                }
                else
                {
                    if (_random.Prob(GetZombieInfectionChance(entity, component)) ||
                        mobState.CurrentState != MobState.Alive)
                    {
                        // On a diceroll or if critical we infect this victim
                        var pending = EnsureComp<PendingZombieComponent>(entity);
                        pending.MaxInfectionLength = _random.NextFloat(0.25f, 1.0f) * component.ZombieInfectionTurnTime;

                        // Our victims inherit our settings, which defines damage and more.
                        pending.Settings = component.VictimSettings ?? component.Settings;

                        // Track who infected this new zombo
                        pending.Family = new ZombieFamily()
                        {
                            Rules = component.Family.Rules, Generation = component.Family.Generation + 1, Infector = uid
                        };

                        EnsureComp<ZombifyOnDeathComponent>(entity);
                        _popup.PopupEntity(Loc.GetString("zombie-bite-infected-victim"), uid, uid);
                    }

                    // Zombify the dead right now. (Usually that occurs on the critical -> dead transition)
                    if (mobState.CurrentState == MobState.Dead)
                    {
                        _zombify.ZombifyEntity(entity);
                        args.BonusDamage = -args.BaseDamage;
                    }
                    else if (mobState.CurrentState == MobState.Alive) //heals when zombies bite live entities
                    {
                        var healingSolution = new Solution();
                        healingSolution.AddReagent("Bicaridine", 1.00); //if OP, reduce/change chem
                        _bloodstream.TryAddToChemicals(args.User, healingSolution);
                    }
                }
            }
        }

        /// <summary>
        ///     This is the function to call if you want to unzombify an entity.
        /// </summary>
        /// <param name="source">the entity having the ZombieComponent</param>
        /// <param name="target">the entity you want to unzombify (different from source in case of cloning, for example)</param>
        /// <remarks>
        ///     this currently only restore the name and skin/eye color from before zombified
        ///     TODO: reverse everything else done in ZombifyEntity
        /// </remarks>
        public bool UnZombify(EntityUid source, EntityUid target, ZombieComponent? zombiecomp)
        {
            if (!Resolve(source, ref zombiecomp))
                return false;

            foreach (var (layer, info) in zombiecomp.BeforeZombifiedCustomBaseLayers)
            {
                _humanoidSystem.SetBaseLayerColor(target, layer, info.Color);
                _humanoidSystem.SetBaseLayerId(target, layer, info.ID);
            }
            _humanoidSystem.SetSkinColor(target, zombiecomp.BeforeZombifiedSkinColor);

            MetaData(target).EntityName = zombiecomp.BeforeZombifiedEntityName;
            return true;
        }

        private void OnZombieCloning(EntityUid uid, ZombieComponent zombiecomp, ref CloningEvent args)
        {
            if (UnZombify(args.Source, args.Target, zombiecomp))
                args.NameHandled = true;
        }
    }
}
