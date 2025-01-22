using Content.Shared.Administration.Logs;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.CombatMode.Pacification;

namespace Content.Shared.Damage.Systems
{
    public abstract partial class SharedDamageOtherOnHitSystem : EntitySystem
    {
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
        [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
        [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly MeleeSoundSystem _meleeSound = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly StaminaSystem _stamina = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DamageOtherOnHitComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
            SubscribeLocalEvent<DamageOtherOnHitComponent, ThrownEvent>(OnThrown);

            SubscribeLocalEvent<ItemToggleDamageOtherOnHitComponent, MapInitEvent>(OnItemToggleMapInit);
            SubscribeLocalEvent<DamageOtherOnHitComponent, ItemToggledEvent>(OnItemToggle);
        }

        /// <summary>
        ///   Inherit stats from MeleeWeapon.
        /// </summary>
        private void OnMapInit(EntityUid uid, DamageOtherOnHitComponent component, MapInitEvent args)
        {
            if (!TryComp<MeleeWeaponComponent>(uid, out var melee))
            {
                RaiseLocalEvent(uid, new DamageOtherOnHitStartupEvent((uid, component)));
                return;
            }

            if (component.Damage.Empty)
                component.Damage = melee.Damage * component.MeleeDamageMultiplier;
            if (component.HitSound == null)
                component.HitSound = melee.HitSound;
            if (component.NoDamageSound == null)
            {
                if (melee.NoDamageSound != null)
                    component.NoDamageSound = melee.NoDamageSound;
                else
                    component.NoDamageSound = new SoundCollectionSpecifier("WeakHit");
            }

            RaiseLocalEvent(uid, new DamageOtherOnHitStartupEvent((uid, component)));
        }

        /// <summary>
        ///   Inherit stats from ItemToggleMeleeWeaponComponent.
        /// </summary>
        private void OnItemToggleMapInit(EntityUid uid, ItemToggleDamageOtherOnHitComponent component, MapInitEvent args)
        {
            if (!TryComp<ItemToggleMeleeWeaponComponent>(uid, out var itemToggleMelee) ||
                !TryComp<DamageOtherOnHitComponent>(uid, out var damage))
                return;

            if (component.ActivatedDamage == null && itemToggleMelee.ActivatedDamage is {} activatedDamage)
                component.ActivatedDamage = activatedDamage * damage.MeleeDamageMultiplier;
            if (component.ActivatedHitSound == null)
                component.ActivatedHitSound = itemToggleMelee.ActivatedSoundOnHit;
            if (component.ActivatedNoDamageSound == null && itemToggleMelee.ActivatedSoundOnHitNoDamage is {} activatedSoundOnHitNoDamage)
                component.ActivatedNoDamageSound = activatedSoundOnHitNoDamage;

            RaiseLocalEvent(uid, new ItemToggleDamageOtherOnHitStartupEvent((uid, component)));
        }

        private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
        {
            if (HasComp<DamageOtherOnHitImmuneComponent>(args.Target) || !TryComp<PhysicsComponent>(uid, out var physics))
                return;

            if (HasComp<PacifiedComponent>(args.Component.Thrower)
                && HasComp<MobStateComponent>(args.Target)
                && component.Damage.AnyPositive())
                return;

            if (component.HitQuantity >= component.MaxHitQuantity)
                return;

            // Ignore thrown items that are too slow
            if (physics.LinearVelocity.LengthSquared() < component.MinVelocity)
                return;

            var modifiedDamage = _damageable.TryChangeDamage(args.Target, GetDamage(uid, component, args.Component.Thrower),
                component.IgnoreResistances, origin: args.Component.Thrower);

            // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
            if (modifiedDamage != null)
            {
                if (HasComp<MobStateComponent>(args.Target))
                    _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {modifiedDamage.GetTotal():damage} damage from collision");

                _meleeSound.PlayHitSound(args.Target, null, SharedMeleeWeaponSystem.GetHighestDamageSound(modifiedDamage, _protoManager), null,
                    component.HitSound, component.NoDamageSound);
            }

            if (modifiedDamage is { Empty: false })
                _color.RaiseEffect(Color.Red, new List<EntityUid>() { args.Target }, Filter.Pvs(args.Target, entityManager: EntityManager));

            if (HasComp<StaminaComponent>(args.Target) && TryComp<StaminaDamageOnHitComponent>(uid, out var stamina))
                _stamina.TakeStaminaDamage(args.Target, stamina.Damage, source: uid, sound: stamina.Sound);

            if (physics.LinearVelocity.LengthSquared() > 0f)
            {
                var direction = physics.LinearVelocity.Normalized();
                _sharedCameraRecoil.KickCamera(args.Target, direction);
            }

            // TODO: If more stuff touches this then handle it after.
            _thrownItem.LandComponent(args.Thrown, args.Component, physics, false);

            if (!HasComp<EmbeddableProjectileComponent>(args.Thrown))
            {
                var newVelocity = physics.LinearVelocity;
                newVelocity.X = -newVelocity.X / 4;
                newVelocity.Y = -newVelocity.Y / 4;
                _physics.SetLinearVelocity(uid, newVelocity, body: physics);
            }

            component.HitQuantity += 1;
        }

        /// <summary>
        ///   Used to update the DamageOtherOnHit component on item toggle.
        /// </summary>
        private void OnItemToggle(EntityUid uid, DamageOtherOnHitComponent component, ItemToggledEvent args)
        {
            if (!TryComp<ItemToggleDamageOtherOnHitComponent>(uid, out var itemToggle))
                return;

            if (args.Activated)
            {
                if (itemToggle.ActivatedDamage is {} activatedDamage)
                {
                    itemToggle.DeactivatedDamage ??= component.Damage;
                    component.Damage = activatedDamage * component.MeleeDamageMultiplier;
                }

                if (itemToggle.ActivatedStaminaCost is {} activatedStaminaCost)
                {
                    itemToggle.DeactivatedStaminaCost ??= component.StaminaCost;
                    component.StaminaCost = activatedStaminaCost;
                }

                itemToggle.DeactivatedHitSound ??= component.HitSound;
                component.HitSound = itemToggle.ActivatedHitSound;

                if (itemToggle.ActivatedNoDamageSound is {} activatedNoDamageSound)
                {
                    itemToggle.DeactivatedNoDamageSound ??= component.NoDamageSound;
                    component.NoDamageSound = activatedNoDamageSound;
                }
            }
            else
            {
                if (itemToggle.DeactivatedDamage is {} deactivatedDamage)
                    component.Damage = deactivatedDamage;

                if (itemToggle.DeactivatedStaminaCost is {} deactivatedStaminaCost)
                    component.StaminaCost = deactivatedStaminaCost;

                component.HitSound = itemToggle.DeactivatedHitSound;

                if (itemToggle.DeactivatedNoDamageSound is {} deactivatedNoDamageSound)
                    component.NoDamageSound = deactivatedNoDamageSound;
            }
        }

        private void OnThrown(EntityUid uid, DamageOtherOnHitComponent component, ThrownEvent args)
        {
            component.HitQuantity = 0;
        }

        /// <summary>
        ///   Gets the total damage a throwing weapon does.
        /// </summary>
        public DamageSpecifier GetDamage(EntityUid uid, DamageOtherOnHitComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component, false))
                return new DamageSpecifier();

            var ev = new GetThrowingDamageEvent(uid, component.Damage, new(), user);
            RaiseLocalEvent(uid, ref ev);

            return DamageSpecifier.ApplyModifierSets(ev.Damage, ev.Modifiers);
        }
    }
}
