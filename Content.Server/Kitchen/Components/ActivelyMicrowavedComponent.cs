namespace Content.Server.Kitchen.Components;

/// <summary>
/// Attached to an object that's actively being microwaved
/// </summary>
[RegisterComponent]
public sealed partial class ActivelyMicrowavedComponent : Component
{
    /// <summary>
    /// The microwave this entity is actively being microwaved by.
    /// </summary>
    [DataField]
    public EntityUid? Microwave;
}
