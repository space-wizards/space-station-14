using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Circulatory.Prototypes;

/// <summary>
/// This is a prototype for defining blood groups (O, A, B, AB, etc.)
/// </summary>
[Prototype]
public sealed partial class BloodTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Which antigens are present in this blood type's blood cells
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<BloodAntigenPrototype>> BloodCellAntigens = new();

    /// <summary>
    /// Which antigens are present in this blood type's blood plasma
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<BloodAntigenPrototype>> PlasmaAntigens = new();
}
