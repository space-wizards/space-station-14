using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Content.Server.Speech.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Disease.Components;
using Content.Server.Weapon.Melee;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Atmos.Components;
using Content.Server.Hands.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Mind.Components;
using Content.Server.Chat.Managers;
using Content.Server.Drone.Components; //future-proofing for borg
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;

namespace Content.Server.Disease
{
    /// <summary>
    /// Handles zombie propagation and inherent zombie traits
    /// </summary>
    public sealed class DiseaseZombieSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHumanoidAppearance = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DiseaseZombieComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DiseaseZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<DiseaseZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        }
        /// <remarks>
        /// I would imagine that if this component got assigned to something other than a mob, it would throw hella errors.
        /// </remarks>
        private void OnComponentInit(EntityUid uid, DiseaseZombieComponent component, ComponentInit args)
        {
            if (!component.ApplyZombieTraits || !HasComp<MobStateComponent>(uid))
                return;

            if (HasComp<DiseaseCarrierComponent>(uid))
                EntityManager.RemoveComponent<DiseaseCarrierComponent>(uid);
            if (HasComp<RespiratorComponent>(uid))
                EntityManager.RemoveComponent<RespiratorComponent>(uid);
            if (HasComp<BarotraumaComponent>(uid))
                EntityManager.RemoveComponent<BarotraumaComponent>(uid);
            if (HasComp<HungerComponent>(uid))
                EntityManager.RemoveComponent<HungerComponent>(uid);
            if (HasComp<ThirstComponent>(uid))
                EntityManager.RemoveComponent<ThirstComponent>(uid);

            EntityManager.EnsureComponent<BloodstreamComponent>(uid, out var bloodstream); //zoms need bloodstream anyway for healing
            _bloodstream.SetBloodLossThreshold(uid, 0f, bloodstream);
            _bloodstream.TryModifyBleedAmount(uid, -bloodstream.BleedAmount, bloodstream);
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            EntityManager.EnsureComponent<ReplacementAccentComponent>(uid).Accent = "zombie";

            if (TryComp<DamageableComponent>(uid, out var comp))
            {
                _damageable.SetDamageModifierSetId(uid, "Zombie", comp);
                _damageable.SetAllDamage(comp, 0);
            }

            // circumvents the 20 damage after being converted.
            // idk a better way of doing this within the meleeHitEvent
            var healingSolution = new Solution();
            healingSolution.AddReagent("Bicaridine", 5.00);
            _bloodstream.TryAddToChemicals(uid, healingSolution);  

            if(TryComp<HumanoidAppearanceComponent>(uid, out var spritecomp))
            {
                //zombie hex color is #aeb87bff
                var color = new Color();
                color.R = 0.70f;
                color.G = 0.72f;
                color.B = 0.48f;
                color.A = 1;

                var oldapp = spritecomp.Appearance;
                var newapp = new HumanoidCharacterAppearance(oldapp.HairStyleId,
                    oldapp.HairColor,
                    oldapp.FacialHairStyleId,
                    oldapp.FacialHairColor,
                    oldapp.EyeColor,
                    color);
                _sharedHumanoidAppearance.UpdateAppearance(uid, newapp);
            }

            if(TryComp<HandsComponent>(uid, out var handcomp))
            {
                foreach (var hand in handcomp.Hands)
                {
                    _sharedHands.TrySetActiveHand(uid, hand.Key);
                    _sharedHands.TryDrop(uid);

                    var pos = EntityManager.GetComponent<TransformComponent>(uid).Coordinates;
                    var virtualItem = EntityManager.SpawnEntity("ZombieClaw", pos);
                    _sharedHands.DoPickup(uid, hand.Value, virtualItem);
                }
            }

            if(TryComp<ContainerManagerComponent>(uid,out var cmcomp))
            {
                foreach (var container in cmcomp.Containers)
                {
                    if (container.Value.ID == "gloves")
                    {
                        foreach (var entity in container.Value.ContainedEntities)
                        {
                            container.Value.Remove(entity);
                        }
                    }
                }
            }

            if(TryComp<MindComponent>(uid, out var mindcomp))
            {
                if (mindcomp.Mind != null && mindcomp.Mind.TryGetSession(out var session))
                {
                    var chatMgr = IoCManager.Resolve<IChatManager>();
                    chatMgr.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));
                }
            }

            uid.PopupMessageEveryone(Loc.GetString("zombie-transform", ("target", uid)));
            if (TryComp<MetaDataComponent>(uid, out var metacomp))
            {
                metacomp.EntityName = Loc.GetString("zombie-name-prefix", ("target", metacomp.EntityName));

                if (!HasComp<GhostRoleMobSpawnerComponent>(uid)) //this specific component gives build test trouble so pop off, ig
                {
                    EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid, out var ghostcomp);
                    ghostcomp.RoleName = metacomp.EntityName;
                    ghostcomp.RoleDescription = Loc.GetString("zombie-role-desc"); 
                    ghostcomp.RoleRules = Loc.GetString("zombie-role-rules");
                }
            }
        }

        /// <summary>
        /// This handles zombie disease transfer when a entity is hit.
        /// This is public because the virtualzombiehand entity needs it
        /// </summary>
        public void OnMeleeHit(EntityUid uid, DiseaseZombieComponent component, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            foreach (EntityUid entity in args.HitEntities)
            {
                if (entity == uid)
                    continue;

                if (!HasComp<MobStateComponent>(entity))
                    continue;

                if (_robustRandom.Prob(component.Probability)&& HasComp<DiseaseCarrierComponent>(entity))
                {
                    _disease.TryAddDisease(entity, "ZombieInfection");
                }

                EntityManager.EnsureComponent<MobStateComponent>(entity, out var mobState);
                if (mobState.IsDead() || mobState.IsCritical()) //dead entities are eautomatically infected. MAYBE: have activated infect ability?
                {
                    EntityManager.EnsureComponent<DiseaseZombieComponent>(entity);
                }
                else if (mobState.IsAlive() && !HasComp<DroneComponent>(entity)) //heals when zombies bite live entities
                {
                    var healingSolution = new Solution();
                    healingSolution.AddReagent("Bicaridine", 1.00); //if OP, reduce/change chem
                    _bloodstream.TryAddToChemicals(args.User, healingSolution);
                }
            }
        }

        private void OnRefreshMovementSpeedModifiers(EntityUid uid, DiseaseZombieComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.SlowAmount, component.SlowAmount);
        }
    }
}
