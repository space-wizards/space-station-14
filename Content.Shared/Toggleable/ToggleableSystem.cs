using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Utility;


namespace Content.Shared.Toggleable;

[UsedImplicitly]
public sealed class ToggleableSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private static ToggleableEnabledEvent _enabledEv = new();
    private static ToggleableDisabledEvent _disabledEv = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleVerb);
        SubscribeLocalEvent<ToggleableComponent, ToggleActionEvent>(OnToggleAction);
    }

    /// <summary>
    ///     Checks an entity for <see cref="ToggleableComponent"/>, and sets it's <see cref="ToggleableComponent.Enabled"/> value accordingly, if the component is present.
    ///     Does not raise any events.
    /// </summary>
    /// <returns><c>true</c> if <see cref="ToggleableComponent.Enabled"/> status was set, regardless of whether the new value is different or not.</returns>
    public bool SetEnabled(EntityUid uid, bool newToggle)
    {
        if (!_entityManager.TryGetComponent<ToggleableComponent>(uid, out var toggleableComponent))
            return false;

        toggleableComponent.Enabled = newToggle;
        return true;
    }

    /// <summary>
    ///     Checks an entity for <see cref="ToggleableComponent"/>, and returns it's <see cref="ToggleableComponent.Enabled"/> value, or <c>true</c> if the component isn't present.
    /// </summary>
    public bool IsEnabled(EntityUid uid)
    {
        if (!_entityManager.TryGetComponent<ToggleableComponent>(uid, out var toggleableComponent))
            return true;

        return toggleableComponent.Enabled;
    }

    /// <summary>
    ///     Raises either <see cref="ToggleableComponent"/> or <see cref="ToggleableComponent"/>
    ///     to invert an entity's <see cref="ToggleableComponent.Enabled"/> value, if the component was present.
    /// </summary>
    /// <returns><c>true</c> if an event was raised.</returns>
    public bool Toggle(EntityUid uid, [NotNullWhen(false)] ref ToggleableComponent? toggleableComponent)
    {
        if (!Resolve(uid, ref toggleableComponent))
            return true;

        Toggle(uid, toggleableComponent);
        return false;
    }

    /// <returns>New value of the specified <see cref="ToggleableComponent.Enabled"/>.</returns>
    public bool Toggle(EntityUid uid, ToggleableComponent component)
    {
        if (!component.Enabled)
            RaiseLocalEvent(uid, ref _enabledEv);
        else
            RaiseLocalEvent(uid, ref _disabledEv);

        return !component.Enabled;
    }
    private void AddToggleVerb(EntityUid uid, ToggleableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.AltVerbAvailable)
            return;

        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () => { Toggle(uid, component); },
            Priority = 1,
            Icon = component.Icon,
            Text = Loc.GetString(component.Text),
        };
        args.Verbs.Add(verb);
    }

    private void OnToggleAction(EntityUid uid, ToggleableComponent component, ToggleActionEvent args)
    {
        Toggle(uid, component);
    }
}
