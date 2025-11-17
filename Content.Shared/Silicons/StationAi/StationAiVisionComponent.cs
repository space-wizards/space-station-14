using Content.Shared.Silicons.StationAi;
using Robust.Shared.GameStates;

namespace Content.Shared.StationAi;

/// <summary>
/// Attached to entities that grant vision to the station AI, such as cameras.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStationAiSystem))]
public sealed partial class StationAiVisionComponent : Component
{
    /// <summary>
    /// Determines whether the entity is actively providing vision to the station AI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Determines whether the entity's vision is blocked by walls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Occluded = true;

    /// <summary>
    /// Determines whether the entity needs to be receiving power to provide vision to the station AI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedsPower = false;

    /// <summary>
    /// Determines whether the entity needs to be anchored to provide vision to the station AI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedsAnchoring = false;

    /// <summary>
    /// Vision range in tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 7.5f;
}
