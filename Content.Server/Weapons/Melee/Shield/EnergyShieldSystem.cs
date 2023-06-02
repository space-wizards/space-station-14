using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Temperature;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Robust.Shared.Player;

namespace Content.Server.Weapons.Melee.EnergyShield;

public sealed class EnergyShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyShieldComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<EnergyShieldComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EnergyShieldComponent, IsHotEvent>(OnIsHotEvent);
        SubscribeLocalEvent<EnergyShieldComponent, EnergyShieldDeactivatedEvent>(TurnOff);
        SubscribeLocalEvent<EnergyShieldComponent, EnergyShieldActivatedEvent>(TurnOn);
    }

    private void OnUseInHand(EntityUid uid, EnergyShieldComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (comp.Activated)
        {
            var ev = new EnergyShieldDeactivatedEvent();
            RaiseLocalEvent(uid, ref ev);
        }
        else
        {
            var ev = new EnergyShieldActivatedEvent();
            RaiseLocalEvent(uid, ref ev);
        }

        UpdateAppearance(uid, comp);
    }

    private void TurnOff(EntityUid uid, EnergyShieldComponent comp, ref EnergyShieldDeactivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, 5, item);
        }

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus -= comp.LitDisarmMalus;
        }

        if (comp.IsSharp)
            RemComp<SharpComponent>(uid);

        _audio.Play(comp.DeActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.DeActivateSound.Params);

        comp.Activated = false;
    }

    private void TurnOn(EntityUid uid, EnergyShieldComponent comp, ref EnergyShieldActivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, 9999, item);
        }

        if (comp.IsSharp)
            EnsureComp<SharpComponent>(uid);

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus += comp.LitDisarmMalus;
        }

        _audio.Play(comp.ActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.ActivateSound.Params);

        comp.Activated = true;
    }

    private void UpdateAppearance(EntityUid uid, EnergyShieldComponent component)
    {
        if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
            return;

        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, component.Activated, appearanceComponent);
    }

    private void OnInteractUsing(EntityUid uid, EnergyShieldComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.ContainsAny("Pulsing"))
            return;

        args.Handled = true;
    }

    private void OnIsHotEvent(EntityUid uid, EnergyShieldComponent energyShield, IsHotEvent args)
    {
        args.IsHot = energyShield.Activated;
    }
}
