using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Respiration.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype]
public sealed partial class BreathingTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public Dictionary<ProtoId<GasPrototype>, float> AbsorbedGases = new();

    [DataField(required: true)]
    public Dictionary<ProtoId<GasPrototype>, float> WasteGases = new();
}
