using Content.Server.Nodes.EntitySystems.Autolinkers;
using Content.Shared.Atmos;
using Content.Shared.Nodes;

namespace Content.Server.Nodes.Components.Autolinkers;

/// <summary>
/// A graph node autoconnector component that forms connections between anchored nodes on adjacent tiles in specific directions.
/// Behaviour is handled by <see cref="PortNodeSystem"/>
/// </summary>
[Access(typeof(DirNodeSystem))]
[RegisterComponent]
public sealed partial class DirNodeComponent : Component
{
    /// <summary>
    /// 
    /// </summary>
    [DataField("baseDirection")]
    [ViewVariables(VVAccess.ReadWrite)]
    public PipeDirection BaseDirection = PipeDirection.None;

    /// <summary>
    /// 
    /// </summary>
    [ViewVariables]
    public PipeDirection CurrentDirection = PipeDirection.None;

    /// <summary>
    /// 
    /// </summary>
    [DataField("rotationEnabled")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RotationEnabled = true;

    /// <summary>
    /// 
    /// </summary>
    [DataField("tag")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Tag = null;

    /// <summary>
    /// 
    /// </summary>
    [DataField("flags")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EdgeFlags Flags = EdgeFlags.None;
}
