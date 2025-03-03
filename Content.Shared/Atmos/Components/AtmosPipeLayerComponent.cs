using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Contains layer data for atmos pipes. These layers allow multiple pipes with the same direction
/// to occupy the same tile without their contents mixing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAtmosPipeLayerSystem))]
public sealed partial class AtmosPipeLayerComponent : Component
{
    /// <summary>
    /// The minimum value a pipe layer can be assigned
    /// </summary>
    public const int MinPipeLayer = 1;

    /// <summary>
    /// The maximum value a pipe layer can be assigned
    /// </summary>
    public const int MaxPipeLayer = 3;

    /// <summary>
    /// Determines which layer the pipe is currently assigned.
    /// Only pipes on the same layer can connect with each other.
    /// </summary>
    [DataField("pipeLayer"), AutoNetworkedField]
    public int CurrentPipeLayer = 2;
}
