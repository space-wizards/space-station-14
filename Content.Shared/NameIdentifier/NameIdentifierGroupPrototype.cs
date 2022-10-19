using Robust.Shared.Prototypes;

namespace Content.Shared.NameIdentifier;

[Prototype("nameIdentifierGroup")]
public readonly record struct NameIdentifierGroupPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    /// <summary>
    ///     Should the identifier become the full name, or just append?
    /// </summary>
    [DataField("fullName")] public readonly bool FullName;

    [DataField("prefix")] public readonly string? Prefix;

    [DataField("maxValue")] public readonly int MaxValue = 999;

    [DataField("minValue")] public readonly int MinValue;
}
