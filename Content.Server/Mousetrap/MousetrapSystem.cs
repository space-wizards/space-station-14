using Content.Server.Damage.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mousetrap;
using Content.Shared.StepTrigger;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Mousetrap;

public sealed class MousetrapSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MousetrapComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MousetrapComponent, BeforeDamageUserOnTriggerEvent>(BeforeDamageOnTrigger);
        SubscribeLocalEvent<MousetrapComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<MousetrapComponent, TriggerEvent>(OnTrigger);
    }

    private void OnUseInHand(EntityUid uid, MousetrapComponent component, UseInHandEvent args)
    {
        component.IsActive = !component.IsActive;
        _popupSystem.PopupEntity(component.IsActive
            ? Loc.GetString("mousetrap-on-activate")
            : Loc.GetString("mousetrap-on-deactivate"),
            uid,
            args.User);

        UpdateVisuals(uid);
    }

    private void OnStepTriggerAttempt(EntityUid uid, MousetrapComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue |= component.IsActive;
    }

    private void BeforeDamageOnTrigger(EntityUid uid, MousetrapComponent component, BeforeDamageUserOnTriggerEvent args)
    {
        if (TryComp(args.Tripper, out PhysicsComponent? physics) && physics.Mass != 0)
        {
            // The idea here is inverse,
            // Small - big damage,
            // Large - small damage
            // yes i punched numbers into a calculator until the graph looked right
            var scaledDamage = -50 * Math.Atan(physics.Mass - component.MassBalance) + (25 * Math.PI);
            args.Damage *= scaledDamage;
        }
    }

    private void OnTrigger(EntityUid uid, MousetrapComponent component, TriggerEvent args)
    {
        component.IsActive = false;
        UpdateVisuals(uid);
    }

    private void UpdateVisuals(EntityUid uid, MousetrapComponent? mousetrap = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref mousetrap, ref appearance, false))
        {
            return;
        }

        _appearance.SetData(uid, MousetrapVisuals.Visual,
            mousetrap.IsActive ? MousetrapVisuals.Armed : MousetrapVisuals.Unarmed, appearance);
    }
}
