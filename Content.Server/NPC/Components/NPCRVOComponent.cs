using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;

/// <summary>
/// Stores data for RVO collision avoidance
/// </summary>
[RegisterComponent]
public sealed class NPCRVOComponent : Component
{
    /// <summary>
    /// Maximum number of dynamic neighbors to consider for collision avoidance.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxNeighbors")]
    public int MaxNeighbors = 5;

    /// <summary>
    /// Time horizon to consider for dynamic neighbor collision
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public float TimeHorizon = 3f;

    /// <summary>
    /// Time horizon to consider for static neighbor collision.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public float ObstacleTimeHorizon = 3f;

    /// <summary>
    /// Range considered for neighbor agents
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("neighborRange")]
    public float NeighborRange = 3f;

    [ViewVariables]
    public readonly HashSet<EntityUid> ObstacleNeighbors = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> AgentNeighbors = new();
}
