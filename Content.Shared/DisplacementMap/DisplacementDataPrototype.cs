using Robust.Shared.Prototypes;

namespace Content.Shared.DisplacementMap;

/// <summary>
/// Prototype wrapper for displacement maps, making it easier to network displacements from server to client.
/// </summary>
[Prototype]
public sealed partial class DisplacementDataPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The displacement map this prototype is referencing.
    /// </summary>
    [DataField(required: true)]
    public DisplacementData Displacement = default!;
}
