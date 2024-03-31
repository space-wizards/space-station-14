
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Blood.Prototypes;

/// <summary>
/// This is a prototype for defining blood in a circulatory system.
/// </summary>
[Prototype()]
public sealed partial class BloodDefinitionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// A dictionary containing all the bloodtypes supported by this blood definition and their chance of being
    /// selected when initially creating a bloodstream
    /// </summary>
    [DataField(required:true)]
    public Dictionary<ProtoId<BloodTypePrototype>, FixedPoint2> BloodTypeDistribution = new();

    public ICollection<ProtoId<BloodTypePrototype>> BloodTypes => BloodTypeDistribution.Keys;
}
