using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;


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

        // This exists if someone wants to use it
        SubscribeLocalEvent<ToggleableComponent, ToggleActionEvent>(OnToggleAction);
    }

    /// <summary>
    ///     Checks an entity for <see cref="ToggleableComponent"/>, and sets it's <see cref="ToggleableComponent.Enabled"/> value accordingly, if the component is present.
    /// </summary>
    /// <returns>Whether the component was present.</returns>
    public bool SetEnabled(EntityUid uid, bool newToggle, [NotNullWhen(true)] out ToggleableComponent? toggleableComponent)
    {
        if (!_entityManager.TryGetComponent<ToggleableComponent>(uid, out toggleableComponent))
            return false;

        SetEnabled(uid, newToggle, toggleableComponent);
        return true;
    }

    /// <inheritdoc cref="IsEnabled"/>
    public bool SetEnabled(EntityUid uid, bool newToggle)
    {
        return SetEnabled(uid, newToggle, out _);
    }

    /// <summary>
    ///     Sets an entity's toggle to specified value, and raises the corresponding event for it.
    /// </summary>
    public void SetEnabled(EntityUid uid, bool newToggle, ToggleableComponent toggleableComponent)
    {
        toggleableComponent.Enabled = newToggle;
        if (newToggle)
            RaiseLocalEvent(uid, ref _enabledEv);
        else
            RaiseLocalEvent(uid, ref _disabledEv);
    }

    /// <summary>
    ///     Checks an entity for <see cref="ToggleableComponent"/>, and returns it's <see cref="ToggleableComponent.Enabled"/> value,
    ///     or <paramref name="defaultValue"/> if the component isn't present.
    /// </summary>
    public bool IsEnabled(EntityUid uid, [NotNullWhen(true)] out ToggleableComponent? toggleableComponent, bool defaultValue = true)
    {
        if (!_entityManager.TryGetComponent<ToggleableComponent>(uid, out toggleableComponent))
            return defaultValue;

        return toggleableComponent.Enabled;
    }

    /// <inheritdoc cref="IsEnabled"/>
    public bool IsEnabled(EntityUid uid, bool defaultValue = true)
    {
        return IsEnabled(uid, out _, defaultValue);
    }

    /// <summary>
    ///     Toggles the entity and raises the corresponding event, if a <see cref="ToggleableComponent"/> is present.
    ///     Returns <paramref name="defaultValue"/> if the component isn't present.
    /// </summary>
    /// <returns><paramref name="defaultValue"/> if the <see cref="ToggleableComponent"/> was missing, true otherwise.</returns>
    public bool Toggle(EntityUid uid, [NotNullWhen(true)] out ToggleableComponent? toggleableComponent, bool defaultValue = true)
    {
        if (!_entityManager.TryGetComponent<ToggleableComponent>(uid, out toggleableComponent))
            return defaultValue;

        Toggle(uid, toggleableComponent);
        return true;
    }

    /// <inheritdoc cref="Toggle"/>
    public bool Toggle(EntityUid uid, bool defaultValue = true)
    {
        return Toggle(uid, out _, defaultValue);
    }

    /// <summary>
    ///     Toggles the entity and raises the corresponding event.
    /// </summary>
    /// <returns>New value of the specified <see cref="ToggleableComponent.Enabled"/>.</returns>
    public bool Toggle(EntityUid uid, ToggleableComponent toggleableComponent)
    {
        var inverseEnabled = !toggleableComponent.Enabled;
        SetEnabled(uid, inverseEnabled, toggleableComponent);

        return inverseEnabled;
    }


    private void AddToggleVerb(EntityUid uid, ToggleableComponent toggleableComponent, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!toggleableComponent.AltVerbAvailable)
            return;

        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => SetEnabled(uid, !toggleableComponent.Enabled),
            Priority = 1,
            Icon = toggleableComponent.Icon,
            Text = Loc.GetString(toggleableComponent.Text),
        });
    }

    private void OnToggleAction(EntityUid uid, ToggleableComponent component, ToggleActionEvent args)
    {
        Toggle(uid, component);
    }
}
