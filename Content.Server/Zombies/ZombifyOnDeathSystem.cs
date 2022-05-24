using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
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

namespace Content.Server.Zombies
{
    /// <summary>
    /// Handles zombie propagation and inherent zombie traits
    /// </summary>
    public sealed class ZombifyOnDeathSystem : EntitySystem
    {
        [Dependency] private readonly PolymorphableSystem _polymorph = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly ServerInventorySystem _serverInventory = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHuApp = default!;
        [Dependency] private readonly IChatManager _chatMan = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombifyOnDeathComponent, DamageChangedEvent>(OnDamageChanged);
        }

        /// <summary>
        /// Handles an entity turning into a zombie when they die or go into crit
        /// </summary>
        private void OnDamageChanged(EntityUid uid, ZombifyOnDeathComponent component, DamageChangedEvent args)
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
        /// This is the general purpose function to call if you want to zombify
        /// an entity.
        /// </summary>
        /// <param name="target">the entity being zombified</param>
        public void ZombifyEntity(EntityUid target)
        {
            EntityUid zombie;
            /// The reasoning here is that for regular humanoids, doing a zombie polymorph
            /// to a random zombie type is the most interesting way to do it. For things like animals,
            /// the polymorph system doesn't really work at all, so they just use the old system of
            /// hacking off all of the component
            if (TryComp<HumanoidAppearanceComponent>(target, out var huAppComp))
            {
                var foo = _polymorph.PolymorphEntity(target, "ZombieGeneric");
                if (foo == null) return;
                zombie = foo.Value;

                if (!TryComp<HumanoidAppearanceComponent>(zombie, out var zomAppComp))
                    return;
                var skinColor = new Color(0.70f, 0.72f, 0.48f, 1);
                _sharedHuApp.UpdateAppearance(zombie, zomAppComp.Appearance.WithSkinColor(skinColor));
            }
            else
            {
                zombie = target; //if it doesn't polymorph, then the zombie is just the target entity
                RemComp<DiseaseCarrierComponent>(zombie);
                RemComp<RespiratorComponent>(zombie);
                RemComp<BarotraumaComponent>(zombie);
                RemComp<HungerComponent>(zombie);
                RemComp<ThirstComponent>(zombie);

                EnsureComp<ReplacementAccentComponent>(zombie).Accent = "zombie";
                EnsureComp<CombatModeComponent>(zombie);

                _damageable.SetDamageModifierSetId(zombie, "Zombie");
                _bloodstream.SetBloodLossThreshold(zombie, 0f);
            }

            _popupSystem.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), zombie, Filter.Pvs(zombie));
            _serverInventory.TryUnequip(zombie, "gloves", true, true);

            if (TryComp<DamageableComponent>(zombie, out var damageablecomp))
                _damageable.SetAllDamage(damageablecomp, 0);

            if (TryComp<MetaDataComponent>(zombie, out var meta))
                meta.EntityName = Loc.GetString("zombie-name-prefix", ("target", meta.EntityName));

            var mindcomp = EnsureComp<MindComponent>(zombie);
            if (mindcomp.Mind != null && mindcomp.Mind.TryGetSession(out var session))
                _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));

            if (!HasComp<GhostRoleMobSpawnerComponent>(zombie) && !mindcomp.HasMind) //this specific component gives build test trouble so pop off, ig
            {
                EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(zombie, out var ghostcomp);
                ghostcomp.RoleName = Loc.GetString("zombie-generic");
                ghostcomp.RoleDescription = Loc.GetString("zombie-role-desc");
                ghostcomp.RoleRules = Loc.GetString("zombie-role-rules");
            }

            foreach (var hand in _sharedHands.EnumerateHands(zombie))
            {
                _sharedHands.SetActiveHand(zombie, hand);
                _sharedHands.RemoveHand(zombie, hand.Name);
            }
        }
    }
}
