using Content.Shared.Eye.Blinding.EyeProtection; // why aren't tools predicted 🙂
using Content.Shared.Eye.Blinding;
using Content.Shared.StatusEffect;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Server.Tools;

namespace Content.Server.Eye.Blinding.EyeProtection
{
    public sealed class EyeProtectionSystem : EntitySystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedBlindingSystem _blindingSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RequiresEyeProtectionComponent, ToolUseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<RequiresEyeProtectionComponent, WelderToggledEvent>(OnWelderToggled);

            SubscribeLocalEvent<EyeProtectionComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<EyeProtectionComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnUseAttempt(EntityUid uid, RequiresEyeProtectionComponent component, ToolUseAttemptEvent args)
        {
            if (!component.Toggled)
                return;

            if (!HasComp<StatusEffectsComponent>(args.User) || !TryComp<BlindableComponent>(args.User, out var blindable))
                return;

            if (blindable.Sources > 0)
                return;

            var statusTime = (float) component.StatusEffectTime.TotalSeconds - blindable.BlindResistance;

            if (statusTime <= 0)
                return;

            var statusTimeSpan = TimeSpan.FromSeconds(statusTime * (blindable.EyeDamage + 1));
            // Add permanent eye damage if they had zero protection, also scale their temporary blindness by how much they already accumulated.
            if (_statusEffectsSystem.TryAddStatusEffect(args.User, SharedBlindingSystem.BlindingStatusEffect, statusTimeSpan, false, "TemporaryBlindness") && blindable.BlindResistance <= 0)
                _blindingSystem.AdjustEyeDamage(args.User, 1, blindable);
        }
        private void OnWelderToggled(EntityUid uid, RequiresEyeProtectionComponent component, WelderToggledEvent args)
        {
            component.Toggled = args.WelderOn;
        }

        private void OnEquipped(EntityUid uid, EyeProtectionComponent component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing) || clothing.Slots == SlotFlags.PREVENTEQUIP)
                return;

            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            component.IsActive = true;
            if (!TryComp<BlindableComponent>(args.Equipee, out var blindComp))
                return;

            blindComp.BlindResistance += (float) component.ProtectionTime.TotalSeconds;
        }

        private void OnUnequipped(EntityUid uid, EyeProtectionComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;
            component.IsActive = false;
            if (!TryComp<BlindableComponent>(args.Equipee, out var blindComp))
                return;

            blindComp.BlindResistance -= (float) component.ProtectionTime.TotalSeconds;
        }
    }
}
