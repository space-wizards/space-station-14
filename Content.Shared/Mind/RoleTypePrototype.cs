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
    public LocId Name; //{ get; set; } //= "role-type-crew-aligned-name"; TODO:ERRANT

    /// <summary>
    ///     The role's subtype, shown only to admins to help with antag categorization
    /// </summary>
    [DataField]
    public LocId? Subtype;

    /// <summary>
    ///     Font color.
    /// </summary>
    [DataField]
    public Color Color; // { get; private set; } = Color.FromHex("#eeeeee"); TODO:ERRANT
}
