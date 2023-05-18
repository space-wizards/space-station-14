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
using Content.Server.Body.Components;
using Content.Server.Atmos.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Content.Server.Speech.Components;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;
using Content.Shared.Humanoid;
using Content.Server.Temperature.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Movement.Systems;
using Content.Server.Traits.Assorted;

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
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedCombatModeSystem _combat = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
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

                if (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical)
                {
                    // Romerol prevents zombification
                    if (HasComp<UnzombifyComponent>(uid))
                    {
                        RemCompDeferred<PendingZombieComponent>(uid);
                        continue;
                    }

                }

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
                _damageable.TryChangeDamage(uid, comp.Damage * painMultiple, true, false, damage);
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
                    _damageable.TryChangeDamage(uid, comp.Damage, true, false, damage);
                }
                else if (_random.Prob(comp.ZombieReviveChance))
                {
                    // There's a small chance to reverse all the zombie's damage (damage.Damage) in one go
                    _damageable.TryChangeDamage(uid, -damage.Damage, true, false, damage);
                }

                if (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical)
                {
                    // Unzombify if at least 10u of Romerol is present in the bloodstream
                    if (HasComp<UnzombifyComponent>(uid))
                        UnZombify(uid, uid, comp);
                }
            }
        }

        private void OnSleepAttempt(EntityUid uid, ZombieComponent component, ref TryingToSleepEvent args)
        {
            args.Cancelled = true;
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

                if (args.NewMobState == MobState.Dead)
                {
                    // Roll to see if this zombie is not coming back.
                    //   Note that due to damage reductions it takes a lot of hits to gib a zombie without this.
                    if (_random.Prob(component.ZombiePermadeathChance))
                    {
                        // You're dead! No reviving for you.
                        _mobThreshold.SetAllowRevives(uid, false);
                        component.Permadeath = true;
                        _popup.PopupEntity(Loc.GetString("zombie-permadeath"), uid, uid);
                    }
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
                }
                else
                {
                    if (_random.Prob(GetZombieInfectionChance(entity, component)))
                    {
                        var pending = EnsureComp<PendingZombieComponent>(entity);
                        pending.MaxInfectionLength = _random.NextFloat(0.25f, 1.0f) * component.ZombieInfectionTurnTime;
                        EnsureComp<ZombifyOnDeathComponent>(entity);
                    }
                }

                if ((mobState.CurrentState == MobState.Dead || mobState.CurrentState == MobState.Critical)
                    && !HasComp<ZombieComponent>(entity))
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

            // Remove pressure damage immunity, restore breathing, thirst and hunger
            if (TryComp<RespiratorComponent>(target, out var respiratorComp) && zombiecomp.BeforeZombifiedSuffocationThreshold != null)
                respiratorComp.SuffocationThreshold = (float) zombiecomp.BeforeZombifiedSuffocationThreshold;
            if (TryComp<BarotraumaComponent>(target, out var barotraumaComp) && zombiecomp.BeforeZombifiedBarotraumaImmunity != null)
                barotraumaComp.HasImmunity = (bool) zombiecomp.BeforeZombifiedBarotraumaImmunity;
            if (TryComp<HungerComponent>(target, out var hungerComp) && zombiecomp.BeforeZombifiedHungerDecayRate != null)
                hungerComp.BaseDecayRate = (float) zombiecomp.BeforeZombifiedHungerDecayRate;
            if (TryComp<ThirstComponent>(target, out var thirstComp) && zombiecomp.BeforeZombifiedThirstDecayRate != null)
                thirstComp.BaseDecayRate = (float) zombiecomp.BeforeZombifiedThirstDecayRate;

            // Restore the accent if any
            if (zombiecomp.BeforeZombifiedAccent != null && TryComp<ReplacementAccentComponent>(target, out var accentComp))
                accentComp.Accent = zombiecomp.BeforeZombifiedAccent;
            else
                RemComp<ReplacementAccentComponent>(target);

            // Return the pacifist trait if applicable
            if (zombiecomp.BeforeZombifiedPacifist)
                AddComp<PacifistComponent>(target);

            // This is needed for stupid entities that fuck up combat mode component
            // in an attempt to make an entity not attack. This is the easiest way to do it.
            RemComp<CombatModeComponent>(target);
            var combat = AddComp<CombatModeComponent>(target);
            _combat.SetInCombatMode(target, false, combat);

            // Restore the attack animations and range
            if (zombiecomp.BeforeZombifiedClickAnimation != null && zombiecomp.BeforeZombifiedWideAnimation != null && zombiecomp.BeforeZombifiedRange != null)
            {
                var meleeComp = EnsureComp<MeleeWeaponComponent>(target);
                meleeComp.ClickAnimation = zombiecomp.BeforeZombifiedClickAnimation;
                meleeComp.WideAnimation = zombiecomp.BeforeZombifiedWideAnimation;
                meleeComp.Range = (float) zombiecomp.BeforeZombifiedRange;
                // Take away the baller damage if the zombie was a humanoid (sad)
                if (zombiecomp.BeforeZombifiedMeleeDamageSpecifier != null)
                    meleeComp.Damage = zombiecomp.BeforeZombifiedMeleeDamageSpecifier;
                Dirty(meleeComp);
            }
            else
                RemComp<MeleeWeaponComponent>(target);

            // Stop groaning randomly and on taking damage
            if (HasComp<EmoteOnDamageComponent>(target))
                _emoteOnDamage.RemoveEmote(target, "Scream");
            if (HasComp<AutoEmoteComponent>(target))
                _autoEmote.RemoveEmote(target, "ZombieGroan");

            // We have specific stuff for humanoid zombies because they matter more
            if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
            {
                // Restore the looks
                foreach (var (layer, info) in zombiecomp.BeforeZombifiedCustomBaseLayers)
                {
                    _humanoidSystem.SetBaseLayerColor(target, layer, info.Color);
                    _humanoidSystem.SetBaseLayerId(target, layer, info.ID);
                }
                _humanoidSystem.SetSkinColor(target, zombiecomp.BeforeZombifiedSkinColor);
            }

            // Restore the damage taken modifiers
            if (zombiecomp.BeforeZombifiedDamageModifierSetId != null)
                _damageable.SetDamageModifierSetId(target, zombiecomp.BeforeZombifiedDamageModifierSetId);

            // Restore the bloodloss threshold
            if (zombiecomp.BeforeZombifiedBloodlossThreshold != null)
                _bloodstream.SetBloodLossThreshold(target, (float) zombiecomp.BeforeZombifiedBloodlossThreshold);

            // Popup
            _popupSystem.PopupEntity(Loc.GetString("zombie-cured", ("target", target)), target, PopupType.LargeCaution);

            // Restore the vulnerability to cold
            if (TryComp<TemperatureComponent>(target, out var temperatureComp) && zombiecomp.BeforeZombifiedColdDamage != null)
                temperatureComp.ColdDamage = zombiecomp.BeforeZombifiedColdDamage;

            // No longer can revive themselves
            _mobThreshold.SetAllowRevives(target, false);

            // Kindly return the lost hands
            var handsComp = EnsureComp<HandsComponent>(target);
            for (var i = 0; i < zombiecomp.BeforeZombifiedHandCount; i++)
                switch (i)
                {
                    case 0:
                        _sharedHands.AddHand(target, "hand", HandLocation.Left, handsComp);
                        break;
                    case 1:
                        _sharedHands.AddHand(target, "hand", HandLocation.Right, handsComp);
                        break;
                    case 2:
                        _sharedHands.AddHand(target, "hand", HandLocation.Middle, handsComp);
                        break;
                }

            // Return the name
            MetaData(target).EntityName = zombiecomp.BeforeZombifiedEntityName;

            // Remove the zombie components, deferred because these probably called Unzombify
            RemCompDeferred<UnzombifyComponent>(target);
            RemCompDeferred<ZombieComponent>(target);

            // Restore the normal movement speed
            _movementSpeedModifier.RefreshMovementSpeedModifiers(target);
            return true;
        }

        private void OnZombieCloning(EntityUid uid, ZombieComponent zombiecomp, ref CloningEvent args)
        {
            if (UnZombify(args.Source, args.Target, zombiecomp))
                args.NameHandled = true;
        }
    }
}
