using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This is a prototype for a law governing the behavior of silicons.
/// </summary>
[Prototype("siliconLaw")]
public sealed class SiliconLawPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// A locale string which is the actual text of the law.
    /// </summary>
    [DataField("lawString", required: true)]
    public string LawString = string.Empty;

    /// <summary>
    /// Whether or not a borg can state this law.
    /// </summary>
    [DataField("canState")]
    public bool CanState = true;
}
