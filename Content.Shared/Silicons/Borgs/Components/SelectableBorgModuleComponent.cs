using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for <see cref="BorgModuleComponent"/>s that can be "swapped" to, as opposed to having passive effects.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBorgSystem))]
public sealed partial class SelectableBorgModuleComponent : Component
{
    /// <summary>
    /// The sidebar action for swapping to this module.
    /// </summary>
    [DataField("moduleSwapAction")]
    public InstantAction ModuleSwapAction = new()
    {
        DisplayName = "action-name-swap-module",
        Description = "action-desc-swap-module",
        ItemIconStyle = ItemActionIconStyle.BigItem,
        Event = new BorgModuleActionSelectedEvent(),
        UseDelay = TimeSpan.FromSeconds(0.5f)
    };
}

public sealed partial class BorgModuleActionSelectedEvent : InstantActionEvent
{
}

/// <summary>
/// Event raised by-ref on a module when it is selected
/// </summary>
[ByRefEvent]
public readonly record struct BorgModuleSelectedEvent(EntityUid Chassis);

/// <summary>
/// Event raised by-ref on a module when it is deselected.
/// </summary>
[ByRefEvent]
public readonly record struct BorgModuleUnselectedEvent(EntityUid Chassis);
