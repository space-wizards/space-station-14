using Content.Server.Atmos.Components;
using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;

namespace Content.Server.Damage.Systems
{
    [UsedImplicitly]
    public sealed class GodmodeSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GodmodeComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
            SubscribeLocalEvent<GodmodeComponent, BeforeStatusEffectAddedEvent>(OnBeforeStatusEffect);
            SubscribeLocalEvent<GodmodeComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        }

        private void OnBeforeDamageChanged(EntityUid uid, GodmodeComponent component, ref BeforeDamageChangedEvent args)
        {
            args.Cancelled = true;
        }

        private void OnBeforeStatusEffect(EntityUid uid, GodmodeComponent component, ref BeforeStatusEffectAddedEvent args)
        {
            args.Cancelled = true;
        }

        private void OnBeforeStaminaDamage(EntityUid uid, GodmodeComponent component, ref BeforeStaminaDamageEvent args)
        {
            args.Cancelled = true;
        }

        public void EnableGodmode(EntityUid uid)
        {
            var godmode = EnsureComp<GodmodeComponent>(uid);

            if (TryComp<MovedByPressureComponent>(uid, out var moved))
            {
                godmode.WasMovedByPressure = moved.Enabled;
                moved.Enabled = false;
            }

            if (TryComp<DamageableComponent>(uid, out var damageable))
            {
                godmode.OldDamage = new(damageable.Damage);
            }

            // Rejuv to cover other stuff
            RaiseLocalEvent(uid, new RejuvenateEvent());
        }

        public void DisableGodmode(EntityUid uid)
        {
            if (!TryComp<GodmodeComponent>(uid, out var godmode))
                return;

            if (TryComp<MovedByPressureComponent>(uid, out var moved))
            {
                moved.Enabled = godmode.WasMovedByPressure;
            }

            if (!TryComp<DamageableComponent>(uid, out var damageable))
                return;

            if (godmode.OldDamage != null)
            {
                _damageable.SetDamage(uid, damageable, godmode.OldDamage);
            }

            RemComp<GodmodeComponent>(uid);
        }

        /// <summary>
        ///     Toggles godmode for a given entity.
        /// </summary>
        /// <param name="uid">The entity to toggle godmode for.</param>
        /// <returns>true if enabled, false if disabled.</returns>
        public bool ToggleGodmode(EntityUid uid)
        {
            if (HasComp<GodmodeComponent>(uid))
            {
                DisableGodmode(uid);
                return false;
            }

            EnableGodmode(uid);
            return true;
        }
    }
}
