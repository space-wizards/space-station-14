using Content.Shared.Construction.Conditions;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Prototypes;

[Prototype("construction")]
public sealed partial class ConstructionPrototype : IPrototype
{
    [DataField("conditions")] private List<IConstructionCondition> _conditions = new();

    /// <summary>
    ///     Hide from the construction list
    /// </summary>
    [DataField("hide")]
    public bool Hide = false;

    /// <summary>
    ///     Friendly name displayed in the construction GUI.
    /// </summary>
    [DataField("name")]
    public string Name = string.Empty;

    /// <summary>
    ///     "Useful" description displayed in the construction GUI.
    /// </summary>
    [DataField("description")]
    public string Description = string.Empty;

    /// <summary>
    ///     The <see cref="ConstructionGraphPrototype"/> this construction will be using.
    /// </summary>
    [DataField("graph", customTypeSerializer: typeof(PrototypeIdSerializer<ConstructionGraphPrototype>), required: true)]
    public string Graph = string.Empty;

    /// <summary>
    ///     The target <see cref="ConstructionGraphNode"/> this construction will guide the user to.
    /// </summary>
    [DataField("targetNode")]
    public string TargetNode = string.Empty;

    /// <summary>
    ///     The starting <see cref="ConstructionGraphNode"/> this construction will start at.
    /// </summary>
    [DataField("startNode")]
    public string StartNode = string.Empty;

    /// <summary>
    ///     Texture path inside the construction GUI.
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    /// <summary>
    ///     Texture paths used for the construction ghost.
    /// </summary>
    [DataField("layers")]
    private List<SpriteSpecifier>? _layers;

    /// <summary>
    ///     If you can start building or complete steps on impassable terrain.
    /// </summary>
    [DataField("canBuildInImpassable")]
    public bool CanBuildInImpassable { get; private set; }

    /// <summary>
    /// If not null, then this is used to check if the entity trying to construct this is whitelisted.
    /// If they're not whitelisted, hide the item.
    /// </summary>
    [DataField("entityWhitelist")]
    public EntityWhitelist? EntityWhitelist = null;

    [DataField("category")] public string Category { get; private set; } = "";

    [DataField("objectType")] public ConstructionType Type { get; private set; } = ConstructionType.Structure;

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("placementMode")]
    public string PlacementMode = "PlaceFree";

    /// <summary>
    ///     Whether this construction can be constructed rotated or not.
    /// </summary>
    [DataField("canRotate")]
    public bool CanRotate = true;

    /// <summary>
    ///     Construction to replace this construction with when the current one is 'flipped'
    /// </summary>
    [DataField("mirror", customTypeSerializer: typeof(PrototypeIdSerializer<ConstructionPrototype>))]
    public string? Mirror;

    public IReadOnlyList<IConstructionCondition> Conditions => _conditions;
    public IReadOnlyList<SpriteSpecifier> Layers => _layers ?? new List<SpriteSpecifier> { Icon };
}

public enum ConstructionType
{
    Structure,
    Item,
}
