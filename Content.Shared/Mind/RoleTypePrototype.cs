using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Mind;

/// <summary>
///     The core properties of Role Types
/// </summary>
[Prototype, Serializable]
public sealed class RoleTypePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<RoleTypePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    ///     The role's name, displayed to players and admins.
    /// </summary>
    [DataField]
    public LocId? Name;
    // Can't assign the default here because I want it to be inheritable

    /// <summary>
    ///     The role's subtype, shown only to admins to help with antag categorization
    /// </summary>
    [DataField]
    public LocId? Subtype;

    /// <summary>
    ///     Font color.
    /// </summary>
    [DataField]
    public Color? Color;
    // Can't assign the default here because I want it to be inheritable
}
