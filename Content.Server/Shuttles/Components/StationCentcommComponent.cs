// Modified by Ronstation contributor(s), therefore this file is licensed as MIT sublicensed with AGPL-v3.0.
using Robust.Shared.Map;
using Robust.Shared.Prototypes; // Ronstation - modification.
using Robust.Shared.Utility;
using Content.Shared.Random; // Ronstation - modification.
using Content.Shared.Random.Helpers; // Ronstation - modification.

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

    [DataField]
    public ResPath Map = new("/Maps/centcomm.yml");

    // Ronstation - start of modifications.

    /// <summary>
    /// Uses this instead of Map when set
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype>? WeightedMap = "RandomCentcomm";
    // Ronstation - end of modifications.

    /// <summary>
    /// Centcomm entity that was loaded.
    /// </summary>
    [DataField]
    public EntityUid? Entity;

    [DataField]
    public EntityUid? MapEntity;
}
