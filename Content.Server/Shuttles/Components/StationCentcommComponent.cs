using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Spawns Central Command (emergency destination) for a station.
/// </summary>
[RegisterComponent]
public sealed partial class StationCentcommComponent : Component
{
    /// <summary>
    /// Crude shuttle offset spawning.
    /// </summary>
    [DataField]
    public float ShuttleIndex;

    //starlight, support for multiple centcomms to randomly be rolled at roundstart
    [DataField]
    public ResPath[] Maps = { new("/Maps/_Starlight/Centcomms/CC_Outpost_SC17.yml"), new("/Maps/_Starlight/Centcomms/CC_Outpost_G24.yml")};
    /* [DataField]
    public ResPath Map = new("/Maps/_Starlight/centcomm.yml"); */

    /// <summary>
    /// Centcomm entity that was loaded.
    /// </summary>
    [DataField]
    public EntityUid? Entity;

    [DataField]
    public EntityUid? MapEntity;
}
