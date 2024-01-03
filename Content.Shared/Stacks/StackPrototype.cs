using Content.Shared.Localizations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Stacks;

[Prototype("stack")]
public sealed partial class StackPrototype : IPrototype, ISerializationHooks
{
    private ContentLocalizationManager _contentLoc = default!;

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    private string _name = string.Empty;

    /// <summary>
    ///     Human-readable localized name for this stack type e.g. "Steel"
    /// </summary>
    public string Name => _contentLoc.GetStackPrototypeLocalization(ID).Name ?? _name;

    /// <summary>
    ///     An icon that will be used to represent this stack type.
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    ///     The entity id that will be spawned by default from this stack.
    /// </summary>
    [DataField("spawn", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Spawn { get; private set; } = string.Empty;

    /// <summary>
    ///     The maximum amount of things that can be in a stack.
    ///     Can be overriden on <see cref="StackComponent"/>
    ///     if null, simply has unlimited max count.
    /// </summary>
    [DataField("maxCount")]
    public int? MaxCount { get; private set; }

    /// <summary>
    /// The size of an individual unit of this stack.
    /// </summary>
    [DataField("itemSize")]
    public int? ItemSize;

    void ISerializationHooks.AfterDeserialization()
    {
        _contentLoc = IoCManager.Resolve<ContentLocalizationManager>();
    }
}

