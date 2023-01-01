using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Drone.Components;
using Content.Server.Inventory;
using Content.Server.Speech;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Disease.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Zombies
{
    public sealed class ZombieSystem : SharedZombieSystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;
        [Dependency] private readonly ServerInventorySystem _inv = default!;
        [Dependency] private readonly VocalSystem _vocal = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedMobStateSystem _state = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ZombieComponent, MobStateChangedEvent>(OnMobState);
            SubscribeLocalEvent<ActiveZombieComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<ActiveZombieComponent, AttemptSneezeCoughEvent>(OnSneeze);
            SubscribeLocalEvent<ActiveZombieComponent, TryingToSleepEvent>(OnSleepAttempt);
            SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnZombieInit);

        }

        private void OnSleepAttempt(EntityUid uid, ActiveZombieComponent component, ref TryingToSleepEvent args)
        {
            args.Cancelled = true;
        }

        private void OnMobState(EntityUid uid, ZombieComponent component, MobStateChangedEvent args)
        {
            if (args.CurrentMobState == DamageState.Alive)
                EnsureComp<ActiveZombieComponent>(uid);
            else
                RemComp<ActiveZombieComponent>(uid);
        }

        private void OnDamage(EntityUid uid, ActiveZombieComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased)
                DoGroan(uid, component);
        }

        private void OnSneeze(EntityUid uid, ActiveZombieComponent component, ref AttemptSneezeCoughEvent args)
        {
            args.Cancelled = true;
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

                if (HasComp<DiseaseCarrierComponent>(entity) && _robustRandom.Prob(GetZombieInfectionChance(entity, component)))
                    _disease.TryAddDisease(entity, "ActiveZombieVirus");

                if (HasComp<ZombieComponent>(entity))
                    args.BonusDamage = -args.BaseDamage * zombieComp.OtherZombieDamageCoefficient;

                if ((mobState.CurrentState == DamageState.Dead || mobState.CurrentState == DamageState.Critical)
                    && !HasComp<ZombieComponent>(entity))
                {
                    _zombify.ZombifyEntity(entity);
                    args.BonusDamage = -args.BaseDamage;
                }
                else if (mobState.CurrentState == DamageState.Alive) //heals when zombies bite live entities
                {
                    var healingSolution = new Solution();
                    healingSolution.AddReagent("Bicaridine", 1.00); //if OP, reduce/change chem
                    _bloodstream.TryAddToChemicals(args.User, healingSolution);
                }
            }
        }

        private void DoGroan(EntityUid uid, ActiveZombieComponent component)
        {
            if (component.LastDamageGroanCooldown > 0)
                return;

            if (_robustRandom.Prob(0.5f)) //this message is never seen by players so it just says this for admins
                // What? Is this REALLY the best way we have of letting admins know there are zombies in a round?
                // [automated maintainer groan]
                _chat.TrySendInGameICMessage(uid, "[automated zombie groan]", InGameICChatType.Speak, false);
            else
                _vocal.TryScream(uid);

            component.LastDamageGroanCooldown = component.GroanCooldown;
        }

        private void OnZombieInit(EntityUid ent, ZombieComponent component, ComponentStartup _)
        {
            // Brute
            component.HealingDamageSpecifier.DamageDict["Brute"] = -10;
            component.HealingDamageSpecifier.DamageDict["Pierce"] = -10;
            component.HealingDamageSpecifier.DamageDict["Blunt"] = -10;

            // Burn
            component.HealingDamageSpecifier.DamageDict["Heat"] = -10;
            component.HealingDamageSpecifier.DamageDict["Shock"] = -10;
            component.HealingDamageSpecifier.DamageDict["Cold"] = -10;

            // Zombies do not receive any Toxic or Airloss damage
        }

        /// <summary>
        /// Heals zombie if it is in bad state and not dead
        /// </summary>
        private void TryHealZombie(EntityUid ent, ZombieComponent component)
        {
            if (!TryComp<MobStateComponent>(ent, out var state) || !TryComp<DamageableComponent>(ent, out var damage))
            {
                return;
            }


            if (state.CurrentState == DamageState.Critical)
            {
                _damageable.TryChangeDamage(ent, component.HealingDamageSpecifier, true);
                return;
            }

            if (state.CurrentState != DamageState.Alive)
                return;
            var dmg = damage.TotalDamage;

            // Always zero, I guess?
            var aliveThreshold = 0f;
            var critStateData = _state.GetEarliestCriticalState(state, dmg);
            var nextThreshold = FixedPoint2.Zero;
            if (critStateData != null)
            {
                nextThreshold = critStateData.Value.threshold;
            }
            else
            {
                var deadStateData = _state.GetEarliestDeadState(state, dmg);
                if (deadStateData == null)
                {
                    return;
                }

                nextThreshold = deadStateData.Value.threshold;
            }


            // Healing to 20% of HP. Not too big and not too small.
            var healingThreshold = aliveThreshold + (nextThreshold - aliveThreshold) * 0.8;

            if (dmg > healingThreshold)
            {
                _damageable.TryChangeDamage(ent, component.HealingDamageSpecifier, true);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var zombiecomp in EntityQuery<ZombieComponent>())
            {
                // Only zombies that are alive have this thing
                if (TryComp<ActiveZombieComponent>(zombiecomp.Owner, out var activeComp))
                {

                    activeComp.Accumulator += frameTime;
                    activeComp.LastDamageGroanCooldown -= frameTime;

                    if (activeComp.Accumulator >= activeComp.RandomGroanAttempt)
                    {
                        activeComp.Accumulator -= activeComp.RandomGroanAttempt;

                        if (_robustRandom.Prob(activeComp.GroanChance))
                        {
                            //either do a random accent line or scream
                            DoGroan(zombiecomp.Owner, activeComp);
                        }
                    }
                }

                zombiecomp.HealingAccumulator += frameTime;
                while (zombiecomp.HealingAccumulator > zombiecomp.HealingCooldown)
                {
                    zombiecomp.HealingAccumulator -= zombiecomp.HealingCooldown;
                    TryHealZombie(zombiecomp.Owner, zombiecomp);
                }
            }
        }
    }
}
