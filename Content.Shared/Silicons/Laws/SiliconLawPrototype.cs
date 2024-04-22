using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

[Virtual, DataDefinition]
[Serializable, NetSerializable]
public partial class SiliconLaw : IComparable<SiliconLaw>
{
    /// <summary>
    /// A locale string which is the actual text of the law.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string LawString = string.Empty;

    /// <summary>
    /// The order of the law in the sequence.
    /// Also is the identifier if <see cref="LawIdentifierOverride"/> is null.
    /// </summary>
    /// <remarks>
    /// This is a fixedpoint2 only for the niche case of supporting laws that go between 0 and 1.
    /// Funny.
    /// </remarks>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Order;

    /// <summary>
    /// An identifier that overrides <see cref="Order"/> in the law menu UI.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? LawIdentifierOverride;

    public int CompareTo(SiliconLaw? other)
    {
        if (other == null)
            return -1;

        return Order.CompareTo(other.Order);
    }

    /// <summary>
    /// Return a shallow clone of this law.
    /// </summary>
    public SiliconLaw ShallowClone()
    {
        return new SiliconLaw()
        {
            LawString = LawString,
            Order = Order,
            LawIdentifierOverride = LawIdentifierOverride
        };
    }
}

/// <summary>
/// This is a prototype for a law governing the behavior of silicons.
/// </summary>
[Prototype("siliconLaw")]
[Serializable, NetSerializable]
public sealed partial class SiliconLawPrototype : SiliconLaw, IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
}
