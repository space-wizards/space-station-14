using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for denoting and limiting the movement of the blob's player-controlled reticule
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
[AutoGenerateComponentState]
public sealed partial class BlobMarkerComponent : Component
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
        "ActionBlobCreateResource",
        "ActionBlobCreateFactory",
        "ActionBlobCreateNode"
    };

    [DataField]
    public int RegularBlobCost = 4;

    [DataField]
    public EntProtoId RegularBlobProtoId = "BlobStructure";

    [DataField]
    public EntProtoId CoreProtoId = "BlobStructureCore";
}

[DataDefinition]
public sealed partial class BlobCreateStructureEvent : InstantActionEvent
{
    /// <summary>
    /// The structure that's created.
    /// </summary>
    [DataField]
    public EntProtoId Structure;

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
}
