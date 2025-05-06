namespace Content.Server.Kitchen.Components;

/// <summary>
/// Attached to an object that's actively being microwaved
/// </summary>
[RegisterComponent]
public sealed partial class ActivelyCookedComponent : Component // Starlight-edit: Renamed from ActivelyMicrowavedComponent to ActivelyCookedComponent
{
    /// <summary>
    /// The microwave this entity is actively being microwaved by.
    /// </summary>
    [DataField]
    public EntityUid? Microwave;
}
