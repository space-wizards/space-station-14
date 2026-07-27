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

    // DS14-start
    /// <summary>
    /// Chance for an otherwise visible tile to be provided by this vision source.
    /// The source's own tile remains visible while the chance is above zero.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VisibleTileChance = 1f;

    /// <summary>
    /// Seed used to make partial vision deterministic on the server and clients.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int VisibilitySeed;
    // DS14-end
}
