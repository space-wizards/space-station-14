using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Handles the toggle action and disables stealth when clothing is unequipped.
/// </summary>
public sealed class StealthClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthClothingComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<StealthClothingComponent, ToggleStealthEvent>(OnToggleStealth);
        SubscribeLocalEvent<StealthClothingComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<StealthClothingComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<StealthClothingComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, StealthClothingComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    /// <summary>
    /// Sets the clothing's stealth effect for the user.
    /// </summary>
    /// <returns>True if it was changed, false otherwise</returns>
    public bool SetEnabled(EntityUid uid, EntityUid user, bool enabled, StealthClothingComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Enabled == enabled)
            return false;

        // TODO remove this when clothing unequip on delete is less sus
        // prevent debug assert when ending round and its disabled
        if (MetaData(user).EntityLifeStage >= EntityLifeStage.Terminating)
            return false;

        comp.Enabled = enabled;
        Dirty(uid, comp);

        var stealth = EnsureComp<StealthComponent>(user);
        // slightly visible, but doesn't change when moving so it's ok
        var visibility = enabled ? stealth.MinVisibility + comp.Visibility : stealth.MaxVisibility;
        _stealth.SetVisibility(user, visibility, stealth);
        _stealth.SetEnabled(user, enabled, stealth);
        return true;
    }

    /// <summary>
    /// Raise <see cref="AddStealthActionEvent"/> then add the toggle action if it was not cancelled.
    /// </summary>
    private void OnGetItemActions(EntityUid uid, StealthClothingComponent comp, GetItemActionsEvent args)
    {
        var ev = new AddStealthActionEvent(args.User);
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        args.AddAction(ref comp.ToggleActionEntity, comp.ToggleAction);
    }

    /// <summary>
    /// Raises <see cref="AttemptStealthEvent"/> if enabling.
    /// </summary>
    private void OnToggleStealth(EntityUid uid, StealthClothingComponent comp, ToggleStealthEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (comp.Enabled)
        {
            SetEnabled(uid, user, false, comp);
            return;
        }

        var ev = new AttemptStealthEvent(user);
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        SetEnabled(uid, user, true, comp);
    }

    /// <summary>
    /// Calls <see cref="SetEnabled"/> when server sends new state.
    /// </summary>
    private void OnHandleState(EntityUid uid, StealthClothingComponent comp, ref AfterAutoHandleStateEvent args)
    {
        // SetEnabled checks if it is the same, so change it to before state was received from the server
        var enabled = comp.Enabled;
        comp.Enabled = !enabled;
        var user = Transform(uid).ParentUid;
        SetEnabled(uid, user, enabled, comp);
    }

    /// <summary>
    /// Force unstealths the user, doesnt remove StealthComponent since other things might use it
    /// </summary>
    private void OnUnequipped(EntityUid uid, StealthClothingComponent comp, GotUnequippedEvent args)
    {
        SetEnabled(uid, args.Equipee, false, comp);
    }
}

/// <summary>
/// Raised on the stealth clothing when attempting to add an action.
/// </summary>
public sealed class AddStealthActionEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// User that equipped the stealth clothing.
    /// </summary>
    public EntityUid User;

    public AddStealthActionEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summary>
/// Raised on the stealth clothing when the user is attemping to enable it.
/// </summary>
public sealed class AttemptStealthEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// User that is attempting to enable the stealth clothing.
    /// </summary>
    public EntityUid User;

    public AttemptStealthEvent(EntityUid user)
    {
        User = user;
    }
}
