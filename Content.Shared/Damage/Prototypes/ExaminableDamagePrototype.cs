using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes;

/// <summary>
///     Prototype for examinable damage messages.
/// </summary>
[Prototype("examinableDamage")]
public sealed class ExaminableDamagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     List of damage messages IDs sorted by severity.
    ///     First one describes fully intact entity.
    ///     Last one describes almost destroyed.
    /// </summary>
    [DataField("messages")]
    public string[] Messages = {};
}
