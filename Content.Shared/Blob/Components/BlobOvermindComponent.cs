using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for denoting and limiting the movement of the blob's player-controlled reticule
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
[AutoGenerateComponentState]
public sealed partial class BlobOvermindComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Resource = 60;

    [DataField]
    public int ResourceMax = 100;

    [DataField]
    public int ResourcePassiveGen = 2;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSecond;

    [DataField]
    public List<EntProtoId> Actions = new()
    {
        "ActionBlobJumpCore",
        "ActionBlobCreateResource",
        "ActionBlobCreateFactory",
        "ActionBlobCreateNode",
        "ActionBlobSwapCore",
    };

    [DataField]
    public int RegularBlobCost = 4;

    [DataField]
    public int SwapCoreCost = 80;

    [DataField]
    public EntProtoId RegularBlobProtoId = "BlobStructure";

    [DataField]
    public EntProtoId CoreProtoId = "BlobStructureCore";

    [DataField]
    public ProtoId<AlertPrototype> ResourceAlert = "BlobResource";
}

[DataDefinition]
public sealed partial class BlobCreateStructureEvent : InstantActionEvent
{
    /// <summary>
    /// The structure that's created.
    /// </summary>
    [DataField]
    public List<EntProtoId> Structure = new();

    /// <summary>
    /// The resource cost of creating the structure.
    /// </summary>
    [DataField]
    public int Cost;

    /// <summary>
    /// How close this can be to other structures of the same type.
    /// </summary>
    [DataField]
    public float MinRange;

    /// <summary>
    /// A component used to determine what structures <see cref="MinRange"/> applies to.
    /// </summary>
    [DataField]
    public string RangeComponent;

    /// <summary>
    /// Whether or not this structure must be built within a certain range of a core or node.
    /// </summary>
    [DataField]
    public bool RequiresNode;
}

public sealed partial class BlobJumpToCoreEvent : InstantActionEvent
{

}

public sealed partial class BlobSwapCoreEvent : InstantActionEvent
{

}
