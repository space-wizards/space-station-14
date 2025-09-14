using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Laws.LawFormatCorruptions;

/// <summary>
/// Abstract prototype type for different ways a silicon law format can get corrupted.
/// See <see cref="Shared.Silicons.Laws.SiliconLaw.FlavorFormattedLawString" and implementations./> 
/// </summary>
[Virtual, Prototype]
public partial class LawFormatCorruptionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    public virtual string? ApplyFormatCorruption(string toFormat)
    {
        // Should be abstract, but since it isn't just use NoFormatCorruption logic.
        return null;
    }
}
