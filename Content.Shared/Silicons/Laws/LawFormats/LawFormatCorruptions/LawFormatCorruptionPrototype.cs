using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.LawFormats.LawFormatCorruptions;

/// <summary>
/// Container-prototype for silicon law corruptions.
/// Allows abstract logic of <see cref="LawFormatCorruption"/> to be registered under a common prototype type.
/// </summary>
[Prototype]
public sealed partial class LawFormatCorruptionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LawFormatCorruption? FormatCorruption;

    public ProtoId<LawFormatPrototype>? FormatToApply()
    {
        return FormatCorruption?.FormatToApply();
    }
}
