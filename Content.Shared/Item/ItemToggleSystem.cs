using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Robust.Shared.Player;

namespace Content.Shared.Item;

public sealed class ItemToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ItemToggleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleDeactivatedEvent>(TurnOff);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleActivatedEvent>(TurnOn);
    }

    public bool TryActivate(EntityUid uid, ItemToggleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Activated)
            return true;

        var attempt = new ItemToggleActivateAttemptEvent();

        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return false;

        UpdateAppearance(uid, comp);
    }

    private void OnUseInHand(EntityUid uid, ItemToggleComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;


        args.Handled = true;

        Toggle(uid, component);
    }

    private void TurnOff(EntityUid uid, ItemToggleComponent comp, ref ItemToggleDeactivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
            _item.SetSize(uid, comp.OffSize, item);

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
            malus.Malus -= comp.ActivatedDisarmMalus;

        _audio.Play(comp.DeactivateSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, comp.DeactivateSound.Params);

        comp.Activated = false;
    }

    private void TurnOn(EntityUid uid, ItemToggleComponent comp, ref ItemToggleActivatedEvent args)
    {
        if (TryComp(uid, out ItemComponent? item))
            _item.SetSize(uid, comp.OnSize, item);

        if (TryComp<DisarmMalusComponent>(uid, out var malus))
            malus.Malus += comp.ActivatedDisarmMalus;

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
}
