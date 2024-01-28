namespace Content.Server.Shuttles.Components;

/// <summary>
/// Marker that specifies a map as being for FTLing entities.
/// </summary>
[RegisterComponent]
public sealed partial class FTLMapComponent : Component
{
    /// <summary>
    /// Offset for FTLing shuttles so they don't overlap each other.
    /// </summary>
    [DataField]
    public int Index;

    [DataField]
    public string Parallax = "FastSpace";
}
