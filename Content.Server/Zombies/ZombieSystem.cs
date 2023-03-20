using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Cloning;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Drone.Components;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chemistry.Components;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Disease.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Zombies
{
    public sealed class ZombieSystem : SharedZombieSystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;
        [Dependency] private readonly ServerInventorySystem _inv = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
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
            SubscribeLocalEvent<ActiveZombieComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<ActiveZombieComponent, AttemptSneezeCoughEvent>(OnSneeze);
            SubscribeLocalEvent<ActiveZombieComponent, TryingToSleepEvent>(OnSleepAttempt);
        }

        private void OnSleepAttempt(EntityUid uid, ActiveZombieComponent component, ref TryingToSleepEvent args)
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
            //BUG: this won't work when an entity becomes a zombie some other way, such as admin smite
            if (args.NewMobState == MobState.Alive)
            {
                // Groaning when damaged
                EnsureComp<ActiveZombieComponent>(uid);

                // Random groaning
                EnsureComp<AutoEmoteComponent>(uid);
                _autoEmote.AddEmote(uid, "ZombieGroan");
            }
            else
            {
                // Stop groaning when damaged
                RemComp<ActiveZombieComponent>(uid);

                // Stop random groaning
                _autoEmote.RemoveEmote(uid, "ZombieGroan");
            }
        }

        private void OnDamage(EntityUid uid, ActiveZombieComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased)
                AttemptDamageGroan(uid, component);
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

        private void AttemptDamageGroan(EntityUid uid, ActiveZombieComponent component)
        {
            if (component.LastDamageGroan + component.DamageGroanCooldown > _gameTiming.CurTime)
                return;

            if (_robustRandom.Prob(component.DamageGroanChance))
                return;

            _chat.TryEmoteWithoutChat(uid, component.GroanEmoteId);
            component.LastDamageGroan = _gameTiming.CurTime;
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
