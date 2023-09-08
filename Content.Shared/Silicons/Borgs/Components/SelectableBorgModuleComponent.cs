using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for <see cref="BorgModuleComponent"/>s that can be "swapped" to, as opposed to having passive effects.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBorgSystem))]
public sealed partial class SelectableBorgModuleComponent : Component
{
    [DataField("moduleSwapAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ModuleSwapActionId = "ActionBorgSwapModule";

    /// <summary>
    /// The sidebar action for swapping to this module.
    /// </summary>
    [DataField("moduleSwapActionEntity")] public EntityUid? ModuleSwapActionEntity;
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
