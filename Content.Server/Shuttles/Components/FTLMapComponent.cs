namespace Content.Server.Shuttles.Components;

/// <summary>
/// Marker that specifies a map as being for FTLing entities.
/// </summary>
[RegisterComponent]
public sealed partial class FTLMapComponent : Component
{
    [DataField]
    public string Parallax = "FastSpace";
}
