using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Payload.Components;

/// <summary>
///     Component for providing the means of triggering an explosive payload. Used in grenade construction.
/// </summary>
/// <remarks>
///     This component performs two functions. Firstly, it will add or remove other components to some entity when this
///     item is installed inside of it. This is intended for use with constructible grenades. For example, this allows
///     you to add things like <see cref="TimerTriggerComponent"/>, or <see cref="TriggerOnProximityComponent"/>.
///     This is required because otherwise you would have to forward arbitrary interaction directed at the casing
///     through to the trigger, which would be quite complicated. Also proximity triggers don't really work inside of
///     containers.
///
///     Secondly, if the entity that this component is attached to is ever triggered directly (e.g., via a device
///     network message), the trigger will be forwarded to the device that this entity is installed in (if any).
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class PayloadTriggerComponent : Component
{
    /// <summary>
    ///     If true, triggering this entity will also cause the parent of this entity to be triggered.
    /// </summary>
    public bool Active = false;

    /// <summary>
    ///     List of components to add or remove from an entity when this trigger is (un)installed.
    /// </summary>
    [DataField(serverOnly: true, readOnly: true)]
    public ComponentRegistry? Components = null;

    /// <summary>
    ///     Set of components that this trigger has granted to the payload case.
    /// </summary>
    /// <remarks>
    ///     This is used to prevent the accidental removal of components that were never added by this trigger (e.g.,
    ///     because the entity already had the specified component)
    /// </remarks>
    [DataField(serverOnly: true)]
    public HashSet<string> GrantedComponents = new();
}
