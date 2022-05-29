using Robust.Shared.Player;
using Content.Server.Speech.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Disease.Components;
using Content.Server.Disease.Zombie.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Atmos.Components;
using Content.Server.Hands.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Mind.Components;
using Content.Server.Chat.Managers;
using Content.Server.Inventory;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Server.Weapons.Melee.ZombieTransfer.Components;

namespace Content.Server.Disease.Zombie
{
    /// <summary>
    /// Handles zombie propagation and inherent zombie traits
    /// </summary>
    public sealed class DiseaseZombieSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly ServerInventorySystem _serverInventory = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHumanoidAppearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DiseaseZombieComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DiseaseZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        }

        /// <remarks>
        /// I would imagine that if this component got assigned to something other than a mob, it would throw hella errors.
        /// </remarks>
        private void OnComponentInit(EntityUid uid, DiseaseZombieComponent component, ComponentInit args)
        {
            if (!component.ApplyZombieTraits || !HasComp<MobStateComponent>(uid))
                return;

            RemComp<DiseaseCarrierComponent>(uid);
            RemComp<DiseaseBuildupComponent>(uid);
            RemComp<RespiratorComponent>(uid);
            RemComp<BarotraumaComponent>(uid);
            RemComp<HungerComponent>(uid);
            RemComp<ThirstComponent>(uid);

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

            if (TryComp<HumanoidAppearanceComponent>(uid, out var spritecomp))
            {
                var oldapp = spritecomp.Appearance;
                var newapp = oldapp.WithSkinColor(component.SkinColor);
                _sharedHumanoidAppearance.UpdateAppearance(uid, newapp);

                _sharedHumanoidAppearance.ForceAppearanceUpdate(uid);
            }

            if (TryComp<HandsComponent>(uid, out var handcomp))
            {
                foreach (var hand in handcomp.Hands)
                {
                    _sharedHands.TrySetActiveHand(uid, hand.Key);
                    _sharedHands.TryDrop(uid);

                    var pos = EntityManager.GetComponent<TransformComponent>(uid).Coordinates;
                    var claw = EntityManager.SpawnEntity("ZombieClaw", pos);
                    _sharedHands.DoPickup(uid, hand.Value, claw);
                }
            }
            else
            {
                EnsureComp<ZombieTransferComponent>(uid);
            }

            if (TryComp<ServerInventoryComponent>(uid, out var servInvComp))
                _serverInventory.TryUnequip(uid, "gloves", true, true, predicted: false, servInvComp);

            _popupSystem.PopupEntity(Loc.GetString("zombie-transform", ("target", uid)), uid, Filter.Pvs(uid));

            if (TryComp<MetaDataComponent>(uid, out var metacomp))
                metacomp.EntityName = Loc.GetString("zombie-name-prefix", ("target", metacomp.EntityName));

            var mindcomp = EnsureComp<MindComponent>(uid);

            if (mindcomp.Mind != null && mindcomp.Mind.TryGetSession(out var session))
            {
                var chatMgr = IoCManager.Resolve<IChatManager>();
                chatMgr.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));
            }

            if (!HasComp<GhostRoleMobSpawnerComponent>(uid) && !mindcomp.HasMind) //this specific component gives build test trouble so pop off, ig
            {
                EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid, out var ghostcomp);
                ghostcomp.RoleName = Loc.GetString("zombie-generic");
                ghostcomp.RoleDescription = Loc.GetString("zombie-role-desc");
                ghostcomp.RoleRules = Loc.GetString("zombie-role-rules");
            }
        }

        private void OnRefreshMovementSpeedModifiers(EntityUid uid, DiseaseZombieComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.SlowAmount, component.SlowAmount);
        }
    }
}
