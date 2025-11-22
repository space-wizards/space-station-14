using Content.Shared.Machines.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Machines.Components;

/// <summary>
/// Marks an entity as being the owner of a multipart machine.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedMultipartMachineSystem))]
public sealed partial class MultipartMachineComponent : Component
{
    /// <summary>
    /// Dictionary of Enum values to specific parts of this machine.
    /// Each key can be specified as 'enum.<EnumName>.<EnumValue>` in Yaml.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, MachinePart> Parts = [];

    /// <summary>
    /// Whether this multipart machine is assembled or not.
    /// Optional parts are not taken into account.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsAssembled = false;

    /// <summary>
    /// Flag for whether the client side system is allowed to show
    /// ghosts of missing machine parts.
    /// Controlled/Used by the client side.
    /// </summary>
    public List<EntityUid> Ghosts = [];
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class MachinePart
{
    /// <summary>
    /// Component type that is expected for this part to have
    /// to be considered a "Part" of the machine.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ComponentNameSerializer))]
    public string Component = "";

    /// <summary>
    /// Expected offset to find this machine at.
    /// </summary>
    [DataField(required: true)]
    public Vector2i Offset;

    /// <summary>
    /// Whether this part is required for the machine to be
    /// considered "assembled", or is considered an optional extra.
    /// </summary>
    [DataField]
    public bool Optional = false;

    /// <summary>
    /// ID of prototype, used to show sprite and description of part, when user examines the machine and there
    /// is no matched entity. Can reference dummy entities to give more detailed descriptions.
    /// </summary>
    [DataField]
    public EntProtoId? GhostProto = null;

    /// <summary>
    /// Expected rotation for this machine to have.
    /// </summary>
    [DataField]
    public Angle Rotation = Angle.Zero;

    /// <summary>
    /// Network entity, used to inform clients and update their side of the component
    /// locally.
    /// Use the Entity attribute if you wish to get which entity is actually bound to this part.
    /// </summary>
    public NetEntity? NetEntity = null;

    /// <summary>
    /// Entity associated with this part.
    /// Not null when an entity is successfully matched to the part and null otherwise.
    /// </summary>
    [DataField, NonSerialized]
    public EntityUid? Entity = null;

    /// <summary>
    /// Expected graph for this part to use as part of its construction.
    /// </summary>
    [DataField]
    public EntProtoId Graph;

    /// <summary>
    /// Expected node for this part to be in, on the graph.
    /// Used to determine when a construct-able object has been
    /// assembled or disassembled.
    /// </summary>
    [DataField]
    public string ExpectedNode;
}
