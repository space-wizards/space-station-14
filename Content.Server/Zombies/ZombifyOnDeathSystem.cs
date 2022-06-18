using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Server.Disease.Components;
using Content.Server.Body.Components;
using Content.Server.Atmos.Components;
using Content.Server.Nutrition.Components;
using Robust.Shared.Player;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Body.Systems;
using Content.Server.CombatMode;
using Content.Server.Inventory;
using Content.Server.Mind.Components;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Components;
using Content.Server.Mind.Commands;
using Content.Server.Temperature.Components;
using Content.Server.Weapon.Melee.Components;
using Content.Server.Disease;
using Robust.Shared.Containers;
using Content.Shared.Movement.Components;
using Content.Shared.MobState;

namespace Content.Server.Zombies
{
    /// <summary>
    /// Handles zombie propagation and inherent zombie traits
    /// </summary>
    public sealed class ZombifyOnDeathSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly ServerInventorySystem _serverInventory = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHuApp = default!;
        [Dependency] private readonly IChatManager _chatMan = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombifyOnDeathComponent, MobStateChangedEvent>(OnDamageChanged);
        }

        /// <summary>
        /// Handles an entity turning into a zombie when they die or go into crit
        /// </summary>
        private void OnDamageChanged(EntityUid uid, ZombifyOnDeathComponent component, MobStateChangedEvent args)
        {
            if (!TryComp<MobStateComponent>(uid, out var mobstate))
                return;

            if (mobstate.IsDead() ||
                mobstate.IsCritical())
            {
                ZombifyEntity(uid);
            }
        }

        /// <summary>
        /// This is the general purpose function to call if you want to zombify an entity.
        /// It handles both humanoid and nonhumanoid transformation.
        /// </summary>
        /// <param name="target">the entity being zombified</param>
        public void ZombifyEntity(EntityUid target)
        {
            if (HasComp<ZombieComponent>(target))
                return;

            _disease.CureAllDiseases(target);
            RemComp<DiseaseCarrierComponent>(target);
            RemComp<RespiratorComponent>(target);
            RemComp<BarotraumaComponent>(target);
            RemComp<HungerComponent>(target);
            RemComp<ThirstComponent>(target);

            var zombiecomp = EnsureComp<ZombifyOnDeathComponent>(target);
            if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp))
            {
                var appearance = huApComp.Appearance;
                _sharedHuApp.UpdateAppearance(target, appearance.WithSkinColor(zombiecomp.SkinColor), huApComp);
                _sharedHuApp.ForceAppearanceUpdate(target, huApComp);
            }

            if (!HasComp<SharedDummyInputMoverComponent>(target))
                MakeSentientCommand.MakeSentient(target, EntityManager);

            EnsureComp<ReplacementAccentComponent>(target).Accent = "zombie";

            //funny add delet go brrr
            RemComp<CombatModeComponent>(target);
            AddComp<CombatModeComponent>(target);

            var melee = EnsureComp<MeleeWeaponComponent>(target);
            melee.Arc = zombiecomp.AttackArc;
            melee.ClickArc = zombiecomp.AttackArc;
            //lord forgive me for the hardcoded damage
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add("Slash", 13);
            dspec.DamageDict.Add("Piercing", 7);
            melee.Damage = dspec;

            _damageable.SetDamageModifierSetId(target, "Zombie");
            _bloodstream.SetBloodLossThreshold(target, 0f);

            _popupSystem.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, Filter.Pvs(target));
            _serverInventory.TryUnequip(target, "gloves", true, true);

            if (TryComp<TemperatureComponent>(target, out var tempComp))
                tempComp.ColdDamage.ClampMax(0);

            if (TryComp<DamageableComponent>(target, out var damageablecomp))
                _damageable.SetAllDamage(damageablecomp, 0);

            if (TryComp<MetaDataComponent>(target, out var meta))
                meta.EntityName = Loc.GetString("zombie-name-prefix", ("target", meta.EntityName));

            var mindcomp = EnsureComp<MindComponent>(target);
            if (mindcomp.Mind != null && mindcomp.Mind.TryGetSession(out var session))
                _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));     

            if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !mindcomp.HasMind) //this specific component gives build test trouble so pop off, ig
            {
                EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(target, out var ghostcomp);
                ghostcomp.RoleName = Loc.GetString("zombie-generic");
                ghostcomp.RoleDescription = Loc.GetString("zombie-role-desc");
                ghostcomp.RoleRules = Loc.GetString("zombie-role-rules");
            }

            foreach (var hand in _sharedHands.EnumerateHands(target))
            {
                _sharedHands.SetActiveHand(target, hand);
                hand.Container?.EmptyContainer();
                _sharedHands.RemoveHand(target, hand.Name);
            }
            RemComp<HandsComponent>(target);

            EnsureComp<ZombieComponent>(target);
        }
    }
}
