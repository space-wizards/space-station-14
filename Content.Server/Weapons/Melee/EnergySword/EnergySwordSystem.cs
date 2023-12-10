using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Content.Shared.Item;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Melee.EnergySword;

public sealed class EnergySwordSystem : EntitySystem
{
    [Dependency] private readonly SharedRgbLightControllerSystem _rgbSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergySwordComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EnergySwordComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleActivatedEvent>(TurnOn);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleDeactivatedEvent>(TurnOff);
    }

    private void OnMapInit(EntityUid uid, EnergySwordComponent comp, MapInitEvent args)
    {
        if (comp.ColorOptions.Count != 0)
            comp.ActivatedColor = _random.Pick(comp.ColorOptions);

        if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
            return;
        _appearance.SetData(uid, ToggleableLightVisuals.Color, comp.ActivatedColor, appearanceComponent);
    }
    private void TurnOn(EntityUid uid, ItemToggleComponent comp, ref ItemToggleActivatedEvent args)
    {
        if (comp.ActivatedSharp)
            EnsureComp<SharpComponent>(uid);

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus += comp.ActivatedDisarmMalus;
        }
    }
    private void TurnOff(EntityUid uid, ItemToggleComponent comp, ref ItemToggleDeactivatedEvent args)
    {
        var ev = new EnergySwordDeactivatedEvent();
        RaiseLocalEvent(uid, ref ev);
        UpdateAppearance(uid, comp);
    }

    private void TurnOnonWielded(EntityUid uid, EnergySwordComponent comp, ref ItemWieldedEvent args)
    {
        var ev = new EnergySwordActivatedEvent();
        RaiseLocalEvent(uid, ref ev);
        UpdateAppearance(uid, comp);
    }

    private void TurnOff(EntityUid uid, EnergySwordComponent comp, ref EnergySwordDeactivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, "Small", item);
        }

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus -= comp.LitDisarmMalus;
        }

        if (TryComp<MeleeWeaponComponent>(uid, out var weaponComp))
        {
            weaponComp.HitSound = comp.OnHitOff;
            if (comp.Secret)
                weaponComp.Hidden = true;
        }

        if (comp.IsSharp)
            RemComp<SharpComponent>(uid);

        _audio.PlayEntity(comp.DeActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.DeActivateSound.Params);

        comp.Activated = false;
    }

    private void TurnOn(EntityUid uid, EnergySwordComponent comp, ref EnergySwordActivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, "Huge", item);
        }

        if (comp.IsSharp)
            EnsureComp<SharpComponent>(uid);

        if (TryComp<MeleeWeaponComponent>(uid, out var weaponComp))
        {
            weaponComp.HitSound = comp.OnHitOn;
            if (comp.Secret)
                weaponComp.Hidden = false;
        }

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus += comp.LitDisarmMalus;
        }

        _audio.PlayEntity(comp.ActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.ActivateSound.Params);

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
}
