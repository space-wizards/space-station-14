using Content.Server.Damage.Components;
using Content.Server.Damage.Systems;
using Content.Server.Doors;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Sound.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Mousetrap;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Mousetrap;

public sealed class MousetrapSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MousetrapComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MousetrapComponent, BeforeDamageUserOnTriggerEvent>(BeforeDamageOnTrigger);
        SubscribeLocalEvent<MousetrapComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<MousetrapComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<MousetrapComponent, GetVerbsEvent<Verb>>(AddMouseDefuseVerb);
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
            var scaledDamage = CalculateDamage(component, physics);
            args.Damage *= scaledDamage;
        }
    }

    private void OnTrigger(EntityUid uid, MousetrapComponent component, TriggerEvent args)
    {
        component.IsActive = false;
        UpdateVisuals(uid);
    }

    private void AddMouseDefuseVerb(EntityUid uid, MousetrapComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract
            || !args.CanAccess)
            return;

        if (!TryComp(uid, out DamageUserOnTriggerComponent? damageOnTrigger))
            return;

        Verb defuse = new()
        {
            Act = () =>
            {
                if (!component.IsActive)
                {
                    _popupSystem.PopupEntity(Loc.GetString("mousetrap-is-not-activated"), uid, args.User);
                    return;
                }

                _audio.PlayPvs(Comp<EmitSoundOnTriggerComponent>(uid).Sound, uid); // play snAp

                if (_random.Prob(MathF.Min(0.99f, 0.15f * component.Difficulty))) // Chance to die
                {
                    if (TryComp(args.User, out PhysicsComponent? physics) && physics.Mass != 0 && TryComp(args.User, out DamageableComponent? damageable))
                    {
                        var damage = new DamageSpecifier(damageOnTrigger.Damage);
                        damage *= CalculateDamage(component, physics);

                        _damageableSystem.SetDamage(args.User, damageable, damage); // die
                    }
                    _popupSystem.PopupEntity(Loc.GetString("mousetrap-defuse-failed", ("who", Name(args.User))), args.User, Shared.Popups.PopupType.Small);
                    return;
                }
                else if (_random.Prob(MathF.Max(0.01f, 1f - component.Difficulty * 0.2f))) // Chance to get some damage & defuse
                {
                    if (TryComp(args.User, out PhysicsComponent? physics) && physics.Mass != 0 && TryComp(args.User, out DamageableComponent? damageable))
                    {
                        var damage = new DamageSpecifier(damageOnTrigger.Damage);
                        damage *= (CalculateDamage(component, physics) / 4);

                        _damageableSystem.SetDamage(args.User, damageable, damage + damageable.Damage); // add some damage
                    }
                }
                // else the mouse disarmed the mousetrap without damage

                _popupSystem.PopupEntity(Loc.GetString("mousetrap-defused-by", ("who", Name(args.User))), uid, Shared.Popups.PopupType.Medium);
                component.IsActive = false;
                UpdateVisuals(uid);
            },
            Text = Loc.GetString("mousetrap-defuse-verb")
        };
        args.Verbs.Add(defuse);
    }

    private double CalculateDamage(MousetrapComponent component, PhysicsComponent physics)
    {
        return -50 * Math.Atan(physics.Mass - component.MassBalance) + (25 * Math.PI);
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
