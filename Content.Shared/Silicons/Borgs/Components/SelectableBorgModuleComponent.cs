using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for <see cref="BorgModuleComponent"/>s that can be "swapped" to, as opposed to having passive effects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBorgSystem))]
public sealed partial class SelectableBorgModuleComponent : Component
{
    /// <summary>
    /// The sidebar action prototype for swapping to this module.
    /// </summary>
    [DataField]
    public EntProtoId ModuleSwapAction = "ActionBorgSwapModule";

    /// <summary>
    /// The sidebar action entity for swapping to this module.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ModuleSwapActionEntity;
}

/// <summary>
/// Raised on a borg module entity with <see cref="SelectableBorgModuleComponent"/> when a player uses the action provided by it.
/// </summary>
public sealed partial class BorgModuleActionSelectedEvent : InstantActionEvent;

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
