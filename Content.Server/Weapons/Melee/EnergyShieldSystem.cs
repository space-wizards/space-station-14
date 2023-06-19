using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Temperature;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Robust.Shared.Player;

namespace Content.Shared.Weapons.Melee.EnergyShield;

public sealed class EnergyShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ItemToggleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ItemToggleComponent, IsHotEvent>(OnIsHotEvent);
        SubscribeLocalEvent<ItemToggleComponent, EnergyShieldDeactivatedEvent>(TurnOff);
        SubscribeLocalEvent<ItemToggleComponent, EnergyShieldActivatedEvent>(TurnOn);
    }

    private void OnUseInHand(EntityUid uid, ItemToggleComponent comp, UseInHandEvent args)
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

    private void TurnOff(EntityUid uid, ItemToggleComponent comp, ref EnergyShieldDeactivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, comp.OffSize, item);
        }

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus -= comp.ActivatedDisarmMalus;
        }

        if (comp.IsSharp)
            RemComp<SharpComponent>(uid);

        _audio.Play(comp.DeActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.DeActivateSound.Params);

        comp.Activated = false;
    }

    private void TurnOn(EntityUid uid, ItemToggleComponent comp, ref EnergyShieldActivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
        {
            _item.SetSize(uid, comp.OnSize, item);
        }

        if (comp.IsSharp)
            EnsureComp<SharpComponent>(uid);

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
        {
            malus.Malus += comp.ActivatedDisarmMalus;
        }

        _audio.Play(comp.ActivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.ActivateSound.Params);

        comp.Activated = true;
    }

    private void UpdateAppearance(EntityUid uid, ItemToggleComponent component)
    {
        if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
            return;

        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, component.Activated, appearanceComponent);
    }

    private void OnInteractUsing(EntityUid uid, ItemToggleComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.ContainsAny("Pulsing"))
            return;

        args.Handled = true;
    }

    private void OnIsHotEvent(EntityUid uid, ItemToggleComponent itemToggle, IsHotEvent args)
    {
        args.IsHot = itemToggle.Activated;
    }
}
