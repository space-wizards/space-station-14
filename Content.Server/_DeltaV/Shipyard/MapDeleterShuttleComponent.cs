namespace Content.Server._DeltaV.Shipyard;

/// <summary>
/// When added to a shuttle, once it FTLs the previous map is deleted.
/// After that the component is removed to prevent insane abuse.
/// </summary>
/// <remarks>
/// Could be upstreamed at some point, loneop shuttle could use it.
/// </remarks>
[RegisterComponent, Access(typeof(MapDeleterShuttleSystem))]
public sealed partial class MapDeleterShuttleComponent : Component
{
    /// <summary>
    /// Only set by the system to prevent someone in VV deleting maps by mistake or otherwise.
    /// </summary>
    public bool Enabled;
}
