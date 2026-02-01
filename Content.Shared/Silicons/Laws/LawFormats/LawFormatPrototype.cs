using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.LawFormats;

/// <summary>
/// Prototype containing a format to apply to a silicon law.
/// This controls appearance but must never affect the text-as-sentence of the law (meaning).
/// </summary>
[Prototype]
public sealed partial class LawFormatPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LawFormat? Format;

    public string ApplyFormat(string toFormat)
    {
        return Format?.ApplyFormat(toFormat) ?? toFormat;
    }
}
