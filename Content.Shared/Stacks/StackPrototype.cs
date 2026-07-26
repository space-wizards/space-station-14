using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Stacks;

/// <summary>
/// Prototype used to combine and spawn like-entities for <see cref="SharedStackSystem"/>.
/// </summary>
[Prototype]
public sealed partial class StackPrototype : IPrototype, IInheritingPrototype
{
    ///  <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    ///  <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<StackPrototype>))]
    public string[]? Parents { get; private set; }

    ///  <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Human-readable name for this stack type e.g. "Steel"
    /// </summary>
    /// <remarks>This is a localization string ID.</remarks>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    /// <summary>
    /// An icon that will be used to represent this stack type.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// The entity id that will be spawned by default from this stack.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<StackComponent> Spawn { get; private set; } = string.Empty;

    /// <summary>
    /// The maximum amount of things that can be in a stack, can be overriden on <see cref="StackComponent"/>.
    /// If null, simply has unlimited max count.
    /// </summary>
    [DataField]
    public int? MaxCount { get; private set; }
}
