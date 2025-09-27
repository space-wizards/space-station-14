using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

[Virtual, DataDefinition]
[Serializable, NetSerializable]
public partial class SiliconLaw : IComparable<SiliconLaw>, IEquatable<SiliconLaw>
{
    /// <summary>
    /// A locale string which is the underlying base text of the law, before flavor formatting is applied.
    /// If the text should appear corrupted, modify <see cref="FlavorFormattedLawString"/>.
    /// </summary>
    [DataField(required: true)]
    public string LawString = string.Empty;

    /// <summary>
    /// The order of the law in the sequence.
    /// Also is the identifier if <see cref="LawIdentifierOverride"/> is null.
    /// </summary>
    /// <remarks>
    /// This is a fixedpoint2 only for the niche case of supporting laws that go between 0 and 1.
    /// Funny.
    /// </remarks>
    [DataField(required: true)]
    public FixedPoint2 Order;

    /// <summary>
    /// An identifier that overrides <see cref="Order"/> in the law menu UI.
    /// </summary>
    [DataField]
    public string? LawIdentifierOverride;

    /// <summary>
    /// If not null, this string overrides how the law text is presented to the player.
    /// Apply all text flavoring here (i.e. corruption effects).
    /// </summary>
    /// <remarks>
    /// This must only affect presentation:<br/>
    /// - YES: alternative letter-casing of the original text;<br/>
    /// - YES: degraded original lettering (i.e. select letters replaced with similar-looking symbols);<br/>
    /// - YES: original text, but animated;<br/>
    /// - NO: unrelated text that may be understood differently;<br/>
    /// - NO: identical to original text - set this to null instead.
    /// </remarks>
    [DataField]
    public string? FlavorFormattedLawString;

    public string ReadLawString(bool ignoreFlavoring)
    {
        return ignoreFlavoring ? LawString : FlavorFormattedLawString ?? LawString;
    }

    public int CompareTo(SiliconLaw? other)
    {
        if (other == null)
            return -1;

        return Order.CompareTo(other.Order);
    }

    public bool Equals(SiliconLaw? other)
    {
        if (other == null)
            return false;
        return LawString == other.LawString
               && Order == other.Order
               && LawIdentifierOverride == other.LawIdentifierOverride
               && FlavorFormattedLawString == other.FlavorFormattedLawString;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        return Equals(obj as SiliconLaw);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LawString, Order, LawIdentifierOverride, FlavorFormattedLawString);
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
            LawIdentifierOverride = LawIdentifierOverride,
            FlavorFormattedLawString = FlavorFormattedLawString
        };
    }
}

/// <summary>
/// This is a prototype for a law governing the behavior of silicons.
/// </summary>
[Prototype]
public sealed partial class SiliconLawPrototype : SiliconLaw, IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
}
