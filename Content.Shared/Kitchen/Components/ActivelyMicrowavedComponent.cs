using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Attached to an object that's actively being microwaved
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActivelyMicrowavedComponent : Component
{
    /// <summary>
    /// The microwave this entity is actively being microwaved by.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid? Microwave;
}
