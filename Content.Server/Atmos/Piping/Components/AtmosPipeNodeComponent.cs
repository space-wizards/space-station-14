using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Components;

/// <summary>
/// 
/// </summary>
[Access(typeof(AtmosPipeNodeSystem))]
[RegisterComponent]
public sealed partial class AtmosPipeNodeComponent : Component, IGasMixtureHolder
{
    /// <summary>The default volume of the gas mixture inside of this pipe.</summary>
    [ViewVariables]
    public const float DefaultVolume = 200f;

    /// <summary>
    /// The directions, before rotation, in which this pipe can connect to adjacent pipes.
    /// </summary>
    [DataField("pipeDirection")]
    [ViewVariables(VVAccess.ReadWrite)]
    public PipeDirection BasePipeDirection = PipeDirection.None;

    /// <summary>
    /// The current connectable pipe directions (accounting for rotation).
    /// Used to check whether this pipe can connect to pipes in a specific direction.
    /// </summary>
    [ViewVariables]
    public PipeDirection CurrPipeDirection = PipeDirection.None;

    /// <summary>
    /// Whether the directions this node connects in rotates with the node.
    /// </summary>
    [DataField("rotationEnabled")]
    [ViewVariables]
    public bool RotationEnabled = true;

    /// <summary>The volume of the gas mixture inside of this pipe.</summary>
    [DataField("volume")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Volume = DefaultVolume;

    /// <summary>The gas mixture that this pipe node contains.</summary>
    [ViewVariables]
    public GasMixture Air
    {
        // TODO: THIS
        get => PipeNet?.Air ?? GasMixture.SpaceGas;
        set
        {
            DebugTools.Assert(PipeNet != null);
            PipeNet!.Air = value;
        }
    }
}
