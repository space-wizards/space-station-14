using System.Numerics;
using Content.Server.Atmos.Piping.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Piping.Components;

[RegisterComponent, Access(typeof(MultiTilePipingSystem))]
public sealed partial class MultiTilePipingComponent : Component
{
    [DataField]
    public Dictionary<string, MultiTilePipePort> Ports = new();

    [DataField]
    public Dictionary<string, EntityUid> PortEntities = new();
}

[DataDefinition]
public sealed partial class MultiTilePipePort
{
    [DataField]
    public Vector2 Offset;

    [DataField]
    public Angle Rotation;

    [DataField]
    public EntProtoId Prototype = "MultiTilePipePort";

    [DataField]
    public string Node = "pipe";
}
