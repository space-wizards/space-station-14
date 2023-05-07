using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Cloning;
using Content.Server.Drone.Components;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.NPC.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chemistry.Components;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.CombatMode;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Network.Messages;
using Content.Server.Speech.Components;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;
using Content.Server.Temperature.Components;

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
        [Dependency] private readonly FactionSystem _faction = default!;
        [Dependency] private readonly ServerInventorySystem _inv = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
        [Dependency] private readonly EmoteOnDamageSystem _emoteOnDamage = default!;
        [Dependency] private readonly SharedCombatModeSystem _combat = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;

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
        }

        private void OnPendingMapInit(EntityUid uid, PendingZombieComponent component, MapInitEvent args)
        {
            component.NextTick = _timing.CurTime;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var query = EntityQueryEnumerator<PendingZombieComponent>();
            var curTime = _timing.CurTime;

            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.NextTick < curTime)
                    continue;

                comp.NextTick += TimeSpan.FromSeconds(1);
                _damageable.TryChangeDamage(uid, comp.Damage, true, false);
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
            var chance = (max - min) * ((total - items) / total) + min;
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

                if (_random.Prob(GetZombieInfectionChance(entity, component)))
                {
                    EnsureComp<PendingZombieComponent>(entity);
                    EnsureComp<ZombifyOnDeathComponent>(entity);
                }

                if (HasComp<ZombieComponent>(entity))
                    args.BonusDamage = -args.BaseDamage * zombieComp.OtherZombieDamageCoefficient;

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
        public bool UnZombify(EntityUid source, EntityUid target, ZombieComponent? zombiecomp, MobStateComponent? mobState = null)
        {
            if (!Resolve(source, ref zombiecomp))
                return false;

            if (!Resolve(target, ref mobState, logMissing: false))
                return false;

            //Restore eye and skin color
            foreach (var (layer, info) in zombiecomp.BeforeZombifiedCustomBaseLayers)
            {
                _humanoidSystem.SetBaseLayerColor(target, layer, info.Color);
                _humanoidSystem.SetBaseLayerId(target, layer, info.ID);
            }
            _humanoidSystem.SetSkinColor(target, zombiecomp.BeforeZombifiedSkinColor);

            //Restore the name
            MetaData(target).EntityName = zombiecomp.BeforeZombifiedEntityName;

            //Restore the accent
            if (zombiecomp.BeforeZombifiedAccent == "none")
                RemComp<ReplacementAccentComponent>(target);
            else
                EnsureComp<ReplacementAccentComponent>(target).Accent = zombiecomp.BeforeZombifiedAccent;

            //This is needed for stupid entities that fuck up combat mode component
            //in an attempt to make an entity not attack. This is the easiest way to do it.
            RemComp<CombatModeComponent>(target);
            var combat = AddComp<CombatModeComponent>(target);
            _combat.SetInCombatMode(target, false, combat);

            //Restore the original melee damage. We assign the visual appearance
            //and range here because of stuff we'll find out later
            var melee = EnsureComp<MeleeWeaponComponent>(target);
            if (zombiecomp.BeforeZombifiedDamage is not null)
                melee.Damage = zombiecomp.BeforeZombifiedDamage;
            melee.ClickAnimation = zombiecomp.AttackAnimation;
            melee.WideAnimation = zombiecomp.AttackAnimation;
            melee.Range = 1.5f;
            Dirty(melee);

            if (mobState.CurrentState == MobState.Alive)
            {
                // No more groaning when damaged
                EnsureComp<EmoteOnDamageComponent>(target);
                _emoteOnDamage.RemoveEmote(target, "Scream");

                // No more random groaning
                EnsureComp<AutoEmoteComponent>(target);
                _autoEmote.RemoveEmote(target, "ZombieGroan");
            }

            //Restore the bloodloss threshold
            if (zombiecomp.BeforeZombifiedBloodLossThreshold is not null)
                _bloodstream.SetBloodLossThreshold(target, (float) zombiecomp.BeforeZombifiedBloodLossThreshold);

            //Restore the damage taken modifier set
            if (zombiecomp.BeforeZombifiedModifierSetId is not null)
                _damageable.SetDamageModifierSetId(target, zombiecomp.BeforeZombifiedModifierSetId);

            //No more cold immunity
            if (TryComp<TemperatureComponent>(target, out var tempComp) && zombiecomp.BeforeZombifiedColdTempThreshold is not null)
                tempComp.ColdDamage = zombiecomp.BeforeZombifiedColdTempThreshold;

            //Remove the entity from the zombie faction
            _faction.RemoveFaction(target, "Zombie", false);

            //You're officially cured, son.
            RemComp<ZombieComponent>(target);

            return true;
        }

        private void OnZombieCloning(EntityUid uid, ZombieComponent zombiecomp, ref CloningEvent args)
        {
            if (UnZombify(args.Source, args.Target, zombiecomp))
                args.NameHandled = true;
        }
    }
}
