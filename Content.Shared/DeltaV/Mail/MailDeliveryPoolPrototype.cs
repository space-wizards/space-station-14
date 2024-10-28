using Robust.Shared.Prototypes;

namespace Content.Shared.DeltaV.Mail;

/// <summary>
/// Generic random weighting dataset to use.
/// </summary>
[Prototype("mailDeliveryPool")]
public sealed class MailDeliveryPoolPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Mail that can be sent to everyone.
    /// </summary>
    [DataField("everyone")]
    public Dictionary<string, float> Everyone = new();

    /// <summary>
    /// Mail that can be sent only to specific jobs.
    /// </summary>
    [DataField("jobs")]
    public Dictionary<string, Dictionary<string, float>> Jobs = new();

    /// <summary>
    /// Mail that can be sent only to specific departments.
    /// </summary>
    [DataField("departments")]
    public Dictionary<string, Dictionary<string, float>> Departments = new();
}
