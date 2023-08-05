using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;

namespace Content.Shared.Item;

public sealed class ItemToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, UseInHandEvent>(OnUseInHand);
    }

    public void Toggle(EntityUid uid, ItemToggleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Activated)
        {
            TryDeactivate(uid, component: component);
        }
        else
        {
            TryActivate(uid, component: component);
        }
    }

    public bool TryActivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Activated)
            return true;

        var attempt = new ItemToggleActivateAttemptEvent();

        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return false;

        component.Activated = true;
        var ev = new ItemToggleActivatedEvent();
        RaiseLocalEvent(uid, ref ev);
        UpdateAppearance(uid, component);
        Dirty(uid, component);
        _audio.PlayPredicted(component.ActivateSound, uid, user);
        return true;
    }

    public bool TryDeactivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Activated)
            return true;

        var attempt = new ItemToggleDeactivateAttemptEvent();

        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return false;

        component.Activated = false;
        var ev = new ItemToggleDeactivatedEvent();
        RaiseLocalEvent(uid, ref ev);
        UpdateAppearance(uid, component);
        Dirty(uid, component);
        _audio.PlayPredicted(component.DeactivateSound, uid, user);
        return true;
    }

    private void OnUseInHand(EntityUid uid, ItemToggleComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Toggle(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, ItemToggleComponent component)
    {
        if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
            return;

        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, component.Activated, appearanceComponent);
    }
}
