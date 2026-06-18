using Robust.Shared.GameStates;

namespace Content.Shared.Screech;

/// <summary>
/// Protects from the effects of screeches when worn on the HEAD, EARS or EYES slots.
/// Also protects the entity which has this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ScreechProtectionComponent : Component
{
    /// <summary>
    /// If true, a quip will be added to the description detailing its "protection from loud noises"
    /// </summary>
    [DataField]
    public bool ShowInExamine = true;
}
