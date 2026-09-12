using Robust.Shared.Prototypes;

namespace Content.Shared.SpaceLaw;
/// <summary>
/// This is a prototype for a singular law in Space Law.
/// </summary>
[Prototype]
public sealed partial class SpaceLawPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the law.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// A short description of the law that sums up what it's about.
    /// </summary>
    [DataField(required: true)]
    public string Desc = string.Empty;

    /// <summary>
    /// Notes about the law that remark on common scenarios and examples.
    /// </summary>
    [DataField(required: true)]
    public string Notes = string.Empty;

    /// <summary>
    /// The crime code for the law, such as X-01.
    /// </summary>
    [DataField(required: true)]
    public string Code = string.Empty;

    /// <summary>
    /// The color that denotes the grouping of the law, such as violent crimes, rioting crimes, etc.
    /// </summary>
    [DataField(required: true)]
    public string Color = string.Empty;
}
