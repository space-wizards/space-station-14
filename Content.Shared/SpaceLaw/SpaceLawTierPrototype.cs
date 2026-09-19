using Robust.Shared.Prototypes;

namespace Content.Shared.SpaceLaw;

/// <summary>
/// This is a prototype for a given severity tier of space law, such as Minor Crimes, Moderate Crimes, etc.
/// </summary>
[Prototype]
public sealed partial class SpaceLawTierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the tier, such as Minor Crimes.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// The crime code for the tier, such as 2-XX.
    /// </summary>
    [DataField(required: true)]
    public string Code = string.Empty;

    /// <summary>
    /// The maximum brig sentence time that can be applied for the tier, along with any notes.
    /// </summary>
    [DataField(required: true)]
    public string Sentence = string.Empty;

    /// <summary>
    /// The list of laws that are included in a tier.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SpaceLawPrototype>> Laws = new();
}