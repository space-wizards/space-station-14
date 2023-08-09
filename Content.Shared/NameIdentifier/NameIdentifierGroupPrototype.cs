using Robust.Shared.Prototypes;

namespace Content.Shared.NameIdentifier;

[Prototype("nameIdentifierGroup")]
public sealed class NameIdentifierGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     Should the identifier become the full name, or just append?
    /// </summary>
    [DataField("fullName")]
    public bool FullName = false;

    [DataField("prefix")]
    public string? Prefix;

    [DataField("maxValue")]
    public int MaxValue = 1000;

    [DataField("minValue")]
    public int MinValue = 0;
}
