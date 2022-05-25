using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mousetrap;
using Content.Shared.StepTrigger;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Mousetrap;

public sealed class MousetrapSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MousetrapComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MousetrapComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<MousetrapComponent, StepTriggeredEvent>(OnStepTrigger);
    }

    private void Trigger(EntityUid uid, MousetrapComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        component.IsActive = false;
    }

    private void OnUseInHand(EntityUid uid, MousetrapComponent component, UseInHandEvent args)
    {
        component.IsActive = !component.IsActive;

        UpdateVisuals(uid);
    }

    private void OnStepTriggerAttempt(EntityUid uid, MousetrapComponent component, ref StepTriggerAttemptEvent args)
    {
        if (!component.IsActive)
        {
            return;
        }

        if (_tagSystem.HasTag(args.Tripper, "MousetrapSpecialDamage"))
        {
            _damageableSystem.TryChangeDamage(args.Tripper, component.SpecialDamage, true);
            Trigger(uid, component);
            return;
        }

        foreach (var slot in component.IgnoreDamageIfSlotFilled)
        {
            if (!_inventorySystem.TryGetSlotContainer(args.Tripper, slot, out var container, out _))
            {
                continue;
            }

            // Yes, this also means that wearing slippers won't
            // hurt the entity.
            if (container.ContainedEntity != null)
            {
                Trigger(uid, component);
                break;
            }
        }

        args.Continue = component.IsActive;
    }

    private void OnStepTrigger(EntityUid uid, MousetrapComponent component, ref StepTriggeredEvent args)
    {
        Trigger(uid, component);

        UpdateVisuals(uid);
    }

    private void UpdateVisuals(EntityUid uid, MousetrapComponent? mousetrap = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref mousetrap, ref appearance, false))
        {
            return;
        }

        appearance.SetData(MousetrapVisuals.Visual,
            mousetrap.IsActive ? MousetrapVisuals.Armed : MousetrapVisuals.Unarmed);
    }
}
