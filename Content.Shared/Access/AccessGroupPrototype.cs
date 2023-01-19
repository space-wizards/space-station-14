using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Access;

/// <summary>
///     Contains a list of access tags that are part of this group.
///     Used by <see cref="AccessComponent"/> to avoid boilerplate.
/// </summary>
[Prototype("accessGroup")]
public sealed class AccessGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("tags", required: true, customTypeSerializer:typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    public HashSet<string> Tags = default!;
}
