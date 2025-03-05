using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Contains layer data for atmos pipes. Layers allow multiple atmos pipes with the same orientation
/// to be anchored to the same tile without their contents mixing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAtmosPipeLayersSystem))]
public sealed partial class AtmosPipeLayersComponent : Component
{
    /// <summary>
    /// The maximum pipe layer assignable.
    /// </summary>
    public const int MaxPipeLayer = 2;

    /// <summary>
    /// Determines which layer the pipe is currently assigned.
    /// Only pipes on the same layer can connect with each other.
    /// </summary>
    [DataField("pipeLayer"), AutoNetworkedField]
    public int CurrentPipeLayer = 0;

    /// <summary>
    /// An array containing the state names of the different pipe layers.
    /// </summary>
    /// <remarks>
    /// Note: there must be an entry for each pipe layer (from 0 to <see cref="MaxPipeLayer"/>).
    /// </remarks>
    [DataField]
    public string[] LayerVisualStates = new string[MaxPipeLayer + 1];

    [DataField]
    public bool OffsetAboveFloorLayers = false;

    [DataField]
    public bool PipeLayersLocked = false;

    /// <summary>
    /// An array containing the state names of the connectors for the different pipe layers.
    /// </summary>
    /// /// <remarks>
    /// Note: there must be an entry for each pipe layer (from 0 to <see cref="MaxPipeLayer"/>).
    /// </remarks>
    [DataField]
    public string[] ConnectorVisualStates = { "pipeConnector", "pipeConnector1", "pipeConnector2" };
}
