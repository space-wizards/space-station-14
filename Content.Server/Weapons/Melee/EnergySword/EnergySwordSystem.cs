using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Content.Shared.Temperature;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Melee.EnergySword;

public sealed class EnergySwordSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRgbLightControllerSystem _rgbSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergySwordComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EnergySwordComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<EnergySwordComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<EnergySwordComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EnergySwordComponent, IsHotEvent>(OnIsHotEvent);
        SubscribeLocalEvent<EnergySwordComponent, EnergySwordDeactivatedEvent>(TurnOff);
        SubscribeLocalEvent<EnergySwordComponent, EnergySwordActivatedEvent>(TurnOn);
    }

    private void OnMapInit(EntityUid uid, EnergySwordComponent comp, MapInitEvent args)
    {
        if (comp.ColorOptions.Count != 0)
            comp.BladeColor = _random.Pick(comp.ColorOptions);
    }

    private void OnGetMeleeDamage(EntityUid uid, EnergySwordComponent comp, ref GetMeleeDamageEvent args)
    {
        if (!comp.Activated)
            return;

        // Overrides basic blunt damage with burn+slash as set in yaml
        args.Damage = comp.LitDamageBonus;
    }

    private void OnUseInHand(EntityUid uid, EnergySwordComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (comp.Activated)
        {
            var ev = new EnergySwordDeactivatedEvent();
            RaiseLocalEvent(uid, ref ev);
        }
        else
        {
            var ev = new EnergySwordActivatedEvent();
            RaiseLocalEvent(uid, ref ev);
        }

        UpdateAppearance(uid, comp);
    }

    private void TurnOff(EntityUid uid, EnergySwordComponent comp, ref EnergySwordDeactivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, 5, item);
        }

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus -= comp.LitDisarmMalus;
        }

        if (TryComp<MeleeWeaponComponent>(uid, out var weaponComp))
        {
            weaponComp.HitSound = comp.OnHitOff;
            if (comp.Secret)
                weaponComp.HideFromExamine = true;
        }

        if (comp.IsSharp)
            RemComp<SharpComponent>(uid);

        _audio.Play(comp.DeActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.DeActivateSound.Params);

        comp.Activated = false;
    }

    private void TurnOn(EntityUid uid, EnergySwordComponent comp, ref EnergySwordActivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, 9999, item);
        }

        if (comp.IsSharp)
            EnsureComp<SharpComponent>(uid);

        if (TryComp<MeleeWeaponComponent>(uid, out var weaponComp))
        {
            weaponComp.HitSound = comp.OnHitOn;
            if (comp.Secret)
                weaponComp.HideFromExamine = false;
        }

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus += comp.LitDisarmMalus;
        }

        _audio.Play(comp.ActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.ActivateSound.Params);

        comp.Activated = true;
    }

    private void UpdateAppearance(EntityUid uid, EnergySwordComponent component)
    {
        if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
            return;

        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, component.Activated, appearanceComponent);
        _appearance.SetData(uid, ToggleableLightVisuals.Color, component.BladeColor, appearanceComponent);
    }

    private void OnInteractUsing(EntityUid uid, EnergySwordComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.ContainsAny("Pulsing"))
            return;

        args.Handled = true;
        comp.Hacked = !comp.Hacked;

        if (comp.Hacked)
        {
            var rgb = EnsureComp<RgbLightControllerComponent>(uid);
            _rgbSystem.SetCycleRate(uid, comp.CycleRate, rgb);
        }
        else
            RemComp<RgbLightControllerComponent>(uid);
    }

    private void OnIsHotEvent(EntityUid uid, EnergySwordComponent energySword, IsHotEvent args)
    {
        args.IsHot = energySword.Activated;
    }
}
