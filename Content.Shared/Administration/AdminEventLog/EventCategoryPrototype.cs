using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.AdminEventLog;

/// <summary>
/// Prototype of a event category. used to log admin events.
/// </summary>
[Prototype]
public sealed partial class EventCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Name of the event category
    /// </summary>
    [DataField]
    public LocId Title;

    /// <summary>
    /// Description of that type of event.
    /// </summary>
    [DataField]
    public LocId Description;
}
